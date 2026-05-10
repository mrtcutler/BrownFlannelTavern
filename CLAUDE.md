# Brown Flannel Tavern Store - Project Context

ASP.NET Core 9 Razor Pages e-commerce site for the Brown Flannel Tavern, a local tavern in Westland, MI. Freelance/client work, currently in prototype phase. Hosted on Raspberry Pi at `bft.tylercutler.com` via Cloudflare Tunnel. PostgreSQL database. Stripe (test mode for prototype) for payments.

Detailed phase plan and client-facing decision items are tracked in `bft_plan.md` and `client_requirements.md` respectively (both gitignored — local only).

## Conventions

### CSS Styling

Avoid inline `style="..."` attributes on HTML elements. When styling, follow this priority order:

1. **Bootstrap 5 utility class** — check the Bootstrap docs for an existing utility (`mb-3`, `text-center`, `d-flex`, `flex-grow-1`, etc.).
2. **Existing custom class in `wwwroot/css/site.css`** — if Bootstrap doesn't cover it, look for a project-specific class that already exists (`btn-bft`, `navbar-bft`, `bft-*` etc.).
3. **New custom class in `wwwroot/css/site.css`** — if neither covers the case, add a new rule there with a clear, semantic class name.

**Why:** Inline styles fragment styling across files, can't be cleanly overridden without `!important`, and make theme changes painful (e.g., the bft brown/cream palette lives in CSS variables in `site.css` — inline styles bypass it). Centralizing in `site.css` keeps styles discoverable and reusable.

**Existing inline styles in the codebase pre-date this rule.** Don't go on a sweeping cleanup pass, but when you're already editing a file, prefer to extract any inline styles you touch into utility classes or `site.css`.

**Reasonable exceptions** (don't refactor these into CSS classes):
- Values toggled by JavaScript at runtime (e.g., `style="display:none;"` on an element shown/hidden by a script).
- Genuinely per-instance dynamic values (e.g., a progress bar's `style="width: @percent%"`).
