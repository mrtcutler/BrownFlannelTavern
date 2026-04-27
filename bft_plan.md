# Brown Flannel Tavern Store - Project Plan

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

## Phase 3: Email Service ⬜ TO DO
- Integrate email provider (likely Resend — used in SpacedOut project, or evaluate alternatives)
- Generic email service abstraction for easy provider swapping
- Email templates for transactional emails
- Configuration via `appsettings.json`

## Phase 4: Notifications ⬜ TO DO
- **Order Placed Notification**
  - Email customer with order confirmation and summary
  - Email admin users (Owner/Manager) when a new order is placed
- **Tracking Number Entry**
  - Add Tracking Number field to the Order model
  - UI on the Order Details admin page for entering a tracking number
  - Submitting a tracking number automatically updates order status to **Shipped**
- **Shipment Notification**
  - Automatically email the customer when a tracking number is submitted
  - Email includes tracking number and order summary
- **Status Change Notifications**
  - Email customer when order status changes (Processing, Shipped, Delivered)

## Phase 5: Reporting ⬜ TO DO
- **Report Filters**
  - Date range selection (start date / end date)
  - Order status filter (single or multi-select)
- **Export Formats**
  - Generate Excel (.xlsx) document
  - Generate PDF document
- **Report Content** — TBD (details to be refined)
- **Access** — Owner and/or Manager (TBD)

---

## Tech Stack
- **Framework:** ASP.NET Core 9 (Razor Pages)
- **Database:** PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL)
- **Auth:** ASP.NET Core Identity
- **Payments:** Stripe (Stripe.net)
- **Email:** TBD (Phase 3)
