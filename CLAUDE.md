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

### Testing

Tests are a default expectation, not an afterthought. The test project is `BrownFlannelTavernStore.Tests` (xUnit + Moq + FluentAssertions + EF Core InMemory). Folder structure mirrors the main project (`Tests/Services/`, `Tests/Pages/`, etc.).

**Required tests:**
- **Service classes** (`Services/*.cs`) — unit tests for all public methods.
- **PageModel handlers** (`OnGet`, `OnPost`, `OnPostXxx`) with conditional logic — at least one test per branch.
- **Bug fixes** — a regression test that fails before the fix and passes after.
- **Refactors** — must keep all existing tests green; if behavior intentionally changes, update the affected tests in the same commit.

**Don't bother testing:**
- Razor views (`.cshtml` rendering) — fragile, slow, low value at this scale.
- EF Core mappings or framework behavior — already tested by Microsoft.
- Trivial getters/setters on plain models (test computed/derived properties only).

**Conventions:**
- Test class name = `<TypeUnderTest>Tests` (e.g., `CartServiceTests`).
- Test method name = `MethodName_Scenario_ExpectedResult` (e.g., `AddToCart_NewItem_AppendsToCart`).
- Prefer FluentAssertions (`actual.Should().Be(expected)`) over `Assert.Equal` for failure-message clarity.
- Use `[Theory]` + `[InlineData]` for table-driven cases instead of multiple near-duplicate `[Fact]`s.

Run tests from `C:\dev\bft\` with `dotnet test`. CI integration is planned but not yet set up — for now, run tests locally before committing.

### Magic strings: prefer `nameof()`, enums, or static classes

Avoid hardcoded string literals when they refer to a code symbol or a fixed vocabulary of values:

| Instead of | Use |
|---|---|
| `"StatusFilter"` (referring to a property) | `nameof(StatusFilter)` |
| `"Owner"` / `"Manager"` (role names) | `SeedData.OwnerRole` / `SeedData.ManagerRole` constants |
| `"payment_intent.succeeded"` (Stripe event types) | A static class or constant with named values |
| `"email.delivered"` / `"email.bounced"` (Resend event types) | Same — central constants |
| `EmailStatus.Sent.ToString() == "Sent"` | Compare to the enum value directly |

**Why:** Refactoring a property rename automatically updates `nameof()` references but not string literals — so renames silently break route data, attribute bindings, etc. Enums/constants make the set of valid values discoverable in one place and let the compiler catch typos.

**Reasonable exceptions** (don't refactor these):
- User-facing display text and email body copy (the actual sentences).
- One-off log messages with specific contextual content (`_logger.LogError("Failed to send to {Recipient}", email)`).
- C# attribute arguments that can only accept compile-time constants — these often have to stay as string literals if no `const string` is available. If the value appears in 3+ attribute sites, define a `const string` and reference it (`[Authorize(Roles = Roles.OwnerOrManager)]`).
- Test setup data where the string is incidental.

**Grandfathered:** existing code has some `"Owner,Manager"` literals in `[Authorize]` attributes and Stripe/Resend event-type strings. Don't sweep — extract opportunistically when you're already editing the file.
