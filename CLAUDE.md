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

**JS-driven visibility toggles:** also use classes, not inline styles. Toggle Bootstrap's `d-none` via `element.classList.add/remove('d-none')` instead of setting `element.style.display`. Removing the class lets the element return to whatever display the cascade says it should have (block, flex, grid, etc.) — more robust than locking in `display: block`.

**Reasonable exception:** genuinely per-instance dynamic values where a class can't carry the data (e.g., a progress bar's `style="width: @percent%"`, a colored badge whose hex comes from data). These are rare; default to a class.

### CSS units: prefer `rem`

For sizing in `site.css`, prefer `rem` over `px`. Rem scales with the root font size, which respects user accessibility settings (browser default font size, zoom) — px doesn't.

**Use `rem` for:** widths, heights, padding, margin, font sizes, border-radius.
**Use `em` when** you want the value to scale with the *local* font size (e.g., a button's internal padding tracking its own text size).
**Keep `px` for:**
- The base `html { font-size: ... }` declaration (this is the rem reference itself)
- Media query breakpoints (Bootstrap convention)
- 1px hairline borders (`border: 1px solid ...`) — rem can cause subpixel fuzziness
- Box shadows when fractional rem reads awkwardly

Conversion: `1rem` = whatever `html`'s font-size is. With a 16px base, `100px → 6.25rem`. The fractional values aren't ugly — they're exact.
