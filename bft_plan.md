# Brown Flannel Tavern Store - Project Plan

> **Status:** Prototype deployed to `bft.tylercutler.com`. Stripe in test mode using developer's personal account. All Phase 1–2 complete, Phase 3 Iter 1–3 deployed, Phase 4 deployed end-to-end with the unblocked email types. Pending: Phase 3 Iter 4–5, Phases 6–7 (now unblocked per client meeting), Phase 9. Phase 8 declined.
>
> **Open punch list with the client** lives in [`client_requirements.md`](./client_requirements.md). 15 items at last count. Bring that doc to client meetings; answers update this plan.

## Phase 1: Authentication & Roles ✅ COMPLETE
- ASP.NET Core Identity (IdentityDbContext)
- Two roles seeded: **Owner** and **Manager**. Phase 6 will add **Fulfiller** as a third.
- Seed creates default Owner from `AdminSettings:OwnerEmail`/`OwnerPassword` (fail-fast if not configured)
- Login / Logout / Access Denied pages
- Navbar reflects auth state

## Phase 2: Admin Pages ✅ COMPLETE
- **Dashboard** (`/Admin`) — counts + recent orders (Owner + Manager)
- **Product Management** (`/Admin/Products`) — full CRUD, variant management (Owner only)
- **Order Management** (`/Admin/Orders`) — list + status filter + detail view + status updates + notes (Owner + Manager). Sortable columns, filterable by status/order#/date range/customer search. Paginated.
- **User Management** (`/Admin/Users`) — admin account CRUD + role assignment (Owner only). Paginated, sortable, filterable by role.
- **Email Log** (`/Admin/EmailLog`) — paginated audit log of every email sent or attempted, with delivery status from Resend webhooks.

---

## Phase 3: Stripe Test-Mode Checkout + Pickup/Ship + Shipping/Tax 🔄 IN PROGRESS

### Iteration 1 — Test-mode integration ✅ DONE
- Real Stripe test-mode keys wired up
- Stripe Elements card field on `/Checkout/Index`
- End-to-end happy path: cart → form → `confirmCardPayment` → POST → server-side verification → Order created with status `Paid`

### Iteration 2 — Pickup/Ship + Contact Fields + UI Cleanup ✅ DONE
- `FulfillmentMethod` enum (`Pickup` / `Shipped`) on Order
- `Phone` (optional), `NotificationPreference` (dormant — see Phase 8 below) added to Order
- Conditional shipping-address validation by FulfillmentMethod
- Pickup info panel reads from `BusinessSettings.Pickup`
- Sticky-footer flexbox fix, inline-style purge, rem conversion, CSS conventions in CLAUDE.md

### Iteration 3 — Webhooks + Amount Verification ✅ DONE
- `POST /api/stripe/webhook` endpoint with signed-payload verification (Stripe.NET `EventUtility.ConstructEvent`)
- `StripeWebhookService.HandlePaymentIntentSucceededAsync` idempotently marks orders Paid
- Server-side amount verification in `Checkout/Index.cshtml.cs` — refuses to save Order if `paymentIntent.Amount` doesn't match recomputed cart total

### Iteration 4 — Shipping Rate Calculation ⬜ TO DO  *(unblocked pending USPS account + product weights + shipping origin from client)*
- **`IShippingRateProvider` interface** — abstraction over carriers
- **`USPSShippingRateProvider`** implementation:
  - OAuth 2.0 client credentials flow against the post-2026 USPS API
  - Token caching (~8 hour token lifetime)
  - Rate caching by `(originZip, destZip, totalWeight)` for ~5 minutes — protects 60-calls/hr free-tier quota
  - Graceful failure handling (logs + returns null → UI shows "unable to estimate, please contact us")
- **`ProductVariant.WeightOz`** added with seeded placeholder values (6 oz tees, 18 oz hoodies, 4 oz caps) — already in place. Client provides accurate weights via Admin UI.
- **`BusinessSettings.ShippingOrigin`** for the warehouse/ship-from address — separate from `BusinessSettings.Pickup` per client confirmation. Validator already in place.
- Checkout calls `IShippingRateProvider.EstimateAsync` after customer enters shipping address; total updates with shipping cost
- Shipping cost displayed as a separate line in confirmation + emails

### Iteration 5 — Sales Tax via Stripe Tax ✅ DONE
- `PaymentService.CalculateTaxAsync` calls the Stripe Tax Calculations API with `txcd_99030400` (General Apparel) and exclusive tax behavior
- `PaymentService.UpdatePaymentIntentAmountAsync` updates the PaymentIntent to subtotal + tax and stores the `tax_calculation_id` in PI metadata
- Checkout page has an AJAX `OnPostCalculateTax` handler that recomputes tax on shipping-address blur and fulfillment-method toggle; order summary live-updates Subtotal / Tax / Total
- For pickup orders, tax is computed against the configured `BusinessSettings.Pickup` address
- `OnPostAsync` re-runs the tax calculation server-side as the source of truth and refuses the order if `paymentIntent.Amount != (Subtotal + Tax) * 100`
- `Order.Subtotal`, `Order.TaxAmount`, `Order.TaxCalculationId` persisted on every order (migration `20260514141939_AddWeightOzAndOrderTaxColumns` — also catches up the previously-unscaffolded `ProductVariant.WeightOz` column)
- Subtotal + Tax + Total displayed on `Pages/Orders/Confirmation.cshtml`, in the customer order confirmation email, and in the admin "new order" alert email
- **Compliance reminder for client** (already in `client_requirements.md` #14): Stripe Tax *calculates*, doesn't *file*. Client owns the MI sales tax permit and the filing schedule.
- **Deferred (not blocking launch):** post-payment Stripe Tax *Transaction* creation in the webhook handler — needed for Stripe's tax-report exports. Add when Phase 9 (Reporting) is built so the reports tie to Stripe's reconciled view.
- **Not unit-tested:** the new `OnPostCalculateTaxAsync` page handler. `PaymentService` is a concrete class with static Stripe SDK calls; testing the handler's branches would require extracting `IPaymentService` first. The email content (Subtotal/Tax/Total rows) IS covered by tests (4 new cases in `OrderConfirmationEmailTests` / `AdminNewOrderEmailTests`).

---

## Phase 4: Email Service (Resend) ✅ COMPLETE (for unblocked email types)

### Iteration 1 — Foundation ✅ DONE
- `IEmailSender` interface + `ResendEmailSender` implementation calling Resend REST API via `IHttpClientFactory`
- `EmailMessage` record DTO
- Admin-only test page at `/Admin/SendTestEmail`

### Iteration 1.5 — Email logging + audit ✅ DONE
- `EmailLog` model storing full HTML + text bodies for exact-resend support
- `LoggingEmailSender` decorator over `ResendEmailSender`
- Admin viewer at `/Admin/EmailLog/Index`

### Iteration 1.6 — Resend webhooks for delivery status ✅ DONE
- `EmailStatus` includes `Delivered`, `Bounced`, `Complained` (in addition to `Sent`, `Failed`)
- `ResendWebhookService` verifies Svix-signed payload, updates status by `ProviderMessageId`
- Email log UI shows colored status badges + `DeliveryUpdatedAt`

### Iteration 2 — Templates wired to triggers ✅ DONE (for unblocked subset)
- ✅ **Order confirmation** — wired into checkout
- ✅ **Admin "new order" alert** — wired alongside customer confirmation
- ✅ **Order status change** — wired into Admin Order Details (only fires when status actually changes)
- ⏸ **Shipment / tracking notification** — wired with Phase 6
- ⏸ **Refund / cancellation confirmation** — wired with Phase 7

---

## Phase 5: Notifications & Customer Preferences ✅ MOSTLY DONE (rolled into Phase 4 Iter 2)
Originally a separate phase, but the email-template-wiring work happened as vertical slices alongside Phase 4 Iter 2. What's left here is either downstream of Phase 6/7 or vestigial.

- ✅ Order Placed — customer email + admin alert
- ✅ Status Change — customer email
- ⏸ Tracking Number Entered — fires from Phase 6's tracking entry workflow
- ⏸ Refund / Cancellation — fires from Phase 7
- 💤 **Customer notification preference** — `Order.NotificationPreference` field exists but is **dormant**. Original design assumed SMS would be a customer choice. Since Phase 8 (SMS) is declined (see below), there's only one channel (email), so the preference is unused. Field stays in the schema in case SMS is ever re-evaluated; no UI surfaces it.

---

## Phase 6: Fulfillment Workflow ⬜ TO DO  *(direction confirmed by client)*
The "fulfiller" user ships orders and enters tracking numbers. Per client: this might be one person, but could expand to more — needs to be its own role.

- **New `Fulfiller` role** alongside existing Owner/Manager. Add `SeedData.FulfillerRole = "Fulfiller"`. Order management permissions extend to Fulfiller; product/user management stays Owner-only.
- **Multi-tracking support** — confirmed by client that a single order can have multiple tracking numbers (split shipments). New `OrderShipment` table: FK to Order, plus `TrackingNumber`, `Carrier`, `ShippedAt`. One Order has many OrderShipments.
- **Per-order tracking entry** — admin UI on `/Admin/Orders/Details/{id}` to add a new shipment row. Confirmed: skip bulk tracking input for now.
- **Carrier handling** — `Carrier` enum starts with `USPS` only (mirrors `USPSShippingRateProvider` in Phase 3 Iter 4). Dropdown shows a single option today but is extensible — when UPS/FedEx providers are added later, they appear automatically.
- **First tracking submitted** triggers status transition to `Shipped` + customer shipment notification email (Phase 4 Iter 2 deferred item).
- **Pickup orders** skip tracking — admin marks order as **Delivered** (status name pending) when handed over.
- **Status transitions:** `Pending → Paid → Processing → (Shipped | Picked Up) → Delivered`. Or `→ Cancelled` / `→ Refunded` per Phase 7.

---

## Phase 7: Refunds, Cancellations, and Customer Order View ⬜ TO DO  *(direction confirmed by client)*

### 7a — Admin Refund Button
- Owner or Manager (per client decision) can issue refunds from `/Admin/Orders/Details/{id}`
- Refund button calls Stripe Refunds API (`RefundService.CreateAsync`); supports full or partial refund
- Order status updates to `Refunded` (full) or annotated with partial-refund metadata
- Triggers refund confirmation email to customer
- **Confirmed by client:** refunds happen inside the app, not by logging into Stripe dashboard — case-by-case per order

### 7b — Customer Order View (Magic-Link)
- **Magic-link pattern, no customer accounts.** Confirmed by client/developer as the right fit: low-friction for occasional small-business merch buyers; privacy-safe via signed token; avoids account-management UX (signup, password reset, GDPR data export).
- Every order confirmation and status-change email contains a unique URL: `https://bft.tylercutler.com/orders/view?token=<HMAC-signed token>`
- Token is HMAC-signed over the order ID + a secret; non-enumerable. Optionally adds expiry (e.g., 90 days) so old links naturally age out.
- The page shows: order summary, items, status, tracking (when present), pickup info if applicable, and a **Cancel order** button when status is pre-Processing.

### 7c — Customer-Initiated Cancellation
- Cancel button on customer order page (from 7b) is visible only when `OrderStatus` is `Paid` (i.e., before Processing) — confirmed by client
- Clicking it triggers the same Stripe Refund call as 7a (full refund) + order status → `Cancelled` + cancellation confirmation email
- Owner/Manager can still cancel from admin side regardless of status

---

## Phase 8: SMS Notifications ⛔ DECLINED  *(client decision — ongoing cost not worth it)*
Originally deferred pending client decision on A2P 10DLC carrier costs. Client confirmed **no** — the $10–15/mo recurring cost + 1–4 week setup time isn't worth it for BFT's volume.

- `Order.NotificationPreference` field stays in the schema as dormant — if SMS is ever re-evaluated, the data shape is ready and no migration is needed.
- The `INotificationService` / `IEmailSender` abstraction we built remains intact. If a future tenant of the (eventual) template wants SMS, they can implement `ISmsSender` and wire it in without architecting around it.
- **For BFT specifically: deleted from active plan.** Will not be built.

---

## Phase 9: Reporting ⬜ TO DO
- **Filters:** date range, order status (multi-select), fulfillment method (Pickup / Shipped)
- **Export Formats:** Excel (.xlsx), PDF
- **Report Content** — needs client input on what they want to see (sales summary, item breakdown, tax report, etc.)
- **Access** — Owner and/or Manager (TBD)

Probably best implemented after Phase 6 + 7 land since reports want shipment + refund data.

---

## Suggested implementation order

Prioritized by size + dependencies + value-to-client:

1. **Phase 3 Iter 5 (Stripe Tax)** — ~30 min. Smallest change. Enable AutomaticTax flag; pass ship-to address. Unblocks live-launch tax compliance.
2. **Phase 7a (Admin Refund Button)** — ~45 min. Self-contained Stripe Refund API integration on Order Details page. High-value visible admin feature.
3. **Phase 7b (Magic-Link Customer Order View)** — ~90 min. New public page + signed-token URL in emails. Prerequisite for 7c.
4. **Phase 7c (Customer-Initiated Cancel)** — ~30 min on top of 7b. Cancel button on customer order page.
5. **Phase 3 Iter 4 (USPS Shipping Rate Provider)** — ~3–4 hrs. Biggest single chunk. **Blocked until** client provides USPS API account + product weights + shipping origin (requirements #12, #13, #15 in `client_requirements.md`).
6. **Phase 6 (Fulfillment + Fulfiller role + Multi-tracking)** — ~2 hrs. Best done after Phase 3 Iter 4 lands so the tracking entries are meaningful.
7. **Phase 9 (Reporting)** — after Phase 6 + 7 since reports need shipment and refund data. Scope TBD with client.

**Critical path to "live-ready":** items 1, 2, 3, 4, 5 in this order. Total ~6–7 hours of focused work + client-side setup time for the USPS account.

---

## Tech Stack
- **Framework:** ASP.NET Core 9 (Razor Pages)
- **Database:** PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL)
- **Auth:** ASP.NET Core Identity
- **Payments:** Stripe (Stripe.net) — test mode for prototype; **Stripe Tax** for automatic sales tax calculation (Phase 3 Iter 5)
- **Email:** Resend
- **Shipping:** USPS Web Tools (OAuth 2.0 / post-2026 API) — via custom `IShippingRateProvider` abstraction so UPS/FedEx can be added later
- **SMS:** ⛔ declined (see Phase 8)
- **Hosting:** Raspberry Pi (`bft.tylercutler.com` via Cloudflare Tunnel)
- **Testing:** xUnit + Moq + FluentAssertions + EF Core InMemory + AspNetCore.Mvc.Testing (95 tests at last count)
