# Brown Flannel Tavern Store - Project Plan

> **Status:** Prototype, being shown to potential client. Stripe in test mode. Live mode is a separate decision once the client signs.

## Phase 1: Authentication & Roles ✅ COMPLETE
- ASP.NET Core Identity integration (IdentityDbContext)
- Two roles: **Owner** and **Manager**
- Seed data creates default Owner account on startup
- Login / Logout / Access Denied pages
- Navbar shows Login/Logout and Admin links based on auth state
- Role-based authorization on all admin pages
- Default credentials configurable in `appsettings.json`

## Phase 2: Admin Pages ✅ COMPLETE
- **Dashboard** (`/Admin`) — order/product/user counts, recent orders (Owner + Manager)
- **Product Management** (`/Admin/Products`) — full CRUD, variant management (Owner only)
- **Order Management** (`/Admin/Orders`) — list with status filter, detail view, status updates, notes (Owner + Manager)
- **User Management** (`/Admin/Users`) — create/edit/delete admin accounts, role assignment, password reset (Owner only)

---

## Phase 3: Stripe Test-Mode Checkout + Pickup/Ship Option ⬜ TO DO  *(FOUNDATION)*
End-to-end checkout flow with Stripe in test mode. Foundational because everything else (refunds, fulfillment, shipping notifications) depends on real orders existing in the database.

- Wire up real Stripe test-mode keys (replace placeholders in `appsettings.Production.json` on the Pi)
- Stripe Checkout integration via `Stripe.net` (existing `PaymentService` scaffolding)
- Cart → Checkout → Stripe-hosted payment page → success/cancel redirect handling
- Webhook endpoint for `checkout.session.completed` to mark orders paid (separate from client-side success redirect, which can be skipped/refreshed)
- **Pickup vs Shipped fulfillment option** at checkout
  - Customer selects pickup-at-store OR ship-to-address before payment
  - Order model gains `FulfillmentMethod` (enum: `Pickup`, `Shipped`)
  - Shipping address only required when `Shipped`
  - Shipping cost added to total only when `Shipped`
- **Customer contact capture at checkout**: email (required), phone (optional for now — required path for SMS notifications when Phase 8 ships)
- Order data model additions: `FulfillmentMethod`, `ShippingAddress` (nullable), `Phone` (nullable), `NotificationPreference` (default: `Email`)

## Phase 4: Email Service (Resend) 🔄 IN PROGRESS  *(FOUNDATION)*

### Iteration 1 — Foundation ✅ DONE
- ✅ `IEmailSender` interface + `ResendEmailSender` implementation calling Resend's REST API via `IHttpClientFactory` (no third-party SDK)
- ✅ `EmailMessage` record DTO (To, Subject, HtmlBody, EmailType, optional TextBody/OrderId/UserId)
- ✅ Configuration: `Resend:ApiKey`, `Resend:FromAddress`, `Resend:FromName` (kept out of source — `~/secrets/bft/appsettings.Production.json` on the Pi)
- ✅ Admin-only test page at `/Admin/SendTestEmail` for end-to-end integration verification

### Iteration 1.5 — Email logging + audit ✅ DONE
- ✅ `EmailLog` model + `EmailType` / `EmailStatus` enums; `DbSet<EmailLog>` on `StoreDbContext`
- ✅ Stores full HTML and text bodies (≈30 MB/year worst case at BFT volume — negligible) so "resend confirmation" sends the *exact* original message, not a re-rendered template
- ✅ `EmailSendResult` carries Resend's provider message ID for cross-reference
- ✅ `LoggingEmailSender` decorator wraps `ResendEmailSender`; logs every send attempt (sent or failed) to `EmailLogs` table
- ✅ DI: `ResendEmailSender` registered as concrete typed HTTP client; `IEmailSender` resolves to `LoggingEmailSender`
- ✅ Admin Email Log viewer at `/Admin/EmailLog/Index` — shows latest 100 emails with status, links to related order
- ✅ Unit tests: 10 total across `ResendEmailSenderTests` (6) and `LoggingEmailSenderTests` (4) — covers: missing config, successful send returns provider ID, failed send writes Failed log + rethrows, all metadata captured, CreatedAt is current UTC, optional fields nullable

### Iteration 2 — Email templates ⬜ TO DO
Build HTML + plain-text templates for transactional emails. Triggers come in Phase 5.
- Order confirmation
- Shipment / tracking notification
- Order status change
- Refund / cancellation confirmation
- Admin "new order placed" alert

### Note for Phase 8 (deferred)
`ISmsSender` will be added as a sibling interface to `IEmailSender` when SMS unblocks. Higher-level orchestration (decide which channel based on `NotificationPreference`) belongs in Phase 5's notification triggers, not in Phase 4.

## Phase 5: Notifications & Customer Preferences ⬜ TO DO
Built on top of Phases 3 & 4. Channel-agnostic so SMS plugs in cleanly when Phase 8 unblocks.

- **Order Placed**
  - Email customer with order confirmation and summary
  - Email admin users (Owner/Manager) when a new order is placed
- **Status Change**
  - Email customer when order status changes (Processing, Shipped, Delivered, Cancelled, Refunded)
- **Tracking Number Entered** — see Phase 6 for the entry workflow
  - Submitting a tracking number auto-updates order status to **Shipped**
  - Email customer with tracking number, carrier (if known), and order summary
- **Customer notification preference**
  - Default: Email
  - Profile-level setting (Email / SMS / Both) — SMS option visible but disabled until Phase 8
  - Per-order override at checkout (optional)

## Phase 6: Fulfillment Workflow + Bulk Tracking ⬜ TO DO
The "fulfiller" user (initially a single Owner or Manager) ships orders and enters tracking numbers.

- **Fulfiller role / capability** — TBD whether this is a new role or just permissions on existing Owner/Manager (see Open Questions)
- **Single tracking entry**: per-order admin UI on the order detail page (already partially in scope from prior plan)
- **Bulk tracking entry**: dedicated admin page where the fulfiller can paste/enter multiple `OrderId, TrackingNumber, Carrier` rows in one operation
  - Form factor: spreadsheet-style table OR CSV paste OR file upload — TBD
  - Each row triggers the same status-change + notification side effects as single entry
- **Pickup orders** skip tracking — admin marks order as **Picked Up** when handed over
- Status transitions: `Pending → Paid → Processing → (Shipped | Picked Up) → Delivered`

## Phase 7: Refunds & Cancellations ⬜ TO DO
Depends on Phase 3 (Stripe in place) and Phase 4 (email confirmations).

- **Cancellation**
  - Customer self-serve cancel from their order page — TBD whether allowed (Open Questions)
  - Admin can cancel from order detail page
  - Cancellation window: blocked once status reaches `Shipped` / `Picked Up` (TBD)
  - Cancelling a paid order triggers a full refund via Stripe API
- **Refund**
  - Admin-initiated from order detail page
  - Full and partial refund support — partial scope TBD for v1
  - Calls Stripe Refunds API; updates order status to `Refunded` (full) or annotates partial
  - Triggers refund confirmation email to customer

## Phase 8: SMS Notifications ⏸ DEFERRED — pending client decision
Originally planned as a foundation item alongside Email, but **deferred** until client confirms they want to absorb the cost and lead time of US A2P 10DLC registration.

- **Why deferred:** US carriers (since 2023) require A2P 10DLC brand + campaign registration for business SMS. Setup is ~$10–15/mo + per-number fees, with 1–4 weeks of carrier vetting before messages can be sent reliably. Toll-free verification (~3–5 business days) is a faster but more expensive alternative.
- **Provider candidate:** Twilio (best .NET SDK, manages 10DLC paperwork in-product). ~$0.0079/SMS for US toll-free.
- **What's needed once unblocked:**
  - Twilio account + 10DLC (or toll-free) registration
  - `SmsSender` implementation behind the existing `INotificationService` abstraction (added in Phase 4)
  - Customer notification preference UI updated to enable SMS option
  - Required: customer phone number (becomes mandatory in checkout if customer chooses SMS)
- **Use cases the client wants:**
  - SMS alert to fulfiller / admin when a new order is placed
  - SMS option for customers to receive tracking number

## Phase 9: Reporting ⬜ TO DO
- **Report Filters**
  - Date range selection (start date / end date)
  - Order status filter (single or multi-select)
  - Fulfillment method filter (Pickup / Shipped)
- **Export Formats**
  - Excel (.xlsx)
  - PDF
- **Report Content** — TBD (details to be refined with client)
- **Access** — Owner and/or Manager (TBD)

---

## Client Action Items & Open Decisions

All open client-facing items (account setups, product/UX decisions, cost commitments) are tracked in [`client_requirements.md`](./client_requirements.md). Bring that file to client meetings; answers gathered there flow back into this plan to clarify scope on the affected phases.

---

## Tech Stack
- **Framework:** ASP.NET Core 9 (Razor Pages)
- **Database:** PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL)
- **Auth:** ASP.NET Core Identity
- **Payments:** Stripe (Stripe.net) — test mode for prototype
- **Email:** Resend
- **SMS:** Twilio (deferred — see Phase 8)
- **Hosting:** Raspberry Pi (`bft.tylercutler.com` via Cloudflare Tunnel)
