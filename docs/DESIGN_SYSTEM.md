<div class="doc-hero">
<span class="doc-kicker">UBHAYA FASHIONS · BRAND EXPERIENCE</span><span class="doc-pill">Design with consistency</span>
<h1>Design System</h1>
<p>The visual language for a premium, warm and product-first fashion shopping experience across mobile and desktop.</p>
<div class="doc-meta"><div><strong>Direction</strong>Fashion editorial</div><div><strong>Priority</strong>Mobile-first</div><div><strong>Principle</strong>Product before decoration</div></div>
</div>

## <strong>01</strong> Design principles

<div class="doc-grid">
<div class="doc-card pink"><span class="ico">📰</span><strong>Editorial</strong><p>Generous imagery, calm layouts and confident typography.</p></div>
<div class="doc-card purple"><span class="ico">🎯</span><strong>Clarity</strong><p>Price, size, colour, stock and actions are easy to scan.</p></div>
<div class="doc-card blue"><span class="ico">🧱</span><strong>Consistency</strong><p>Shared spacing, components, controls and feedback.</p></div>
<div class="doc-card green"><span class="ico">📱</span><strong>Mobile-first</strong><p>Make the small-screen shopping journey excellent.</p></div>
</div>

## <strong>02</strong> Colour palette

| Token | Value | Role |
|---|---|---|
| Ink | `#242126` | Primary text |
| Muted | `#77717A` | Secondary text |
| Canvas | `#FAF8F7` | Page background |
| Surface | `#FFFFFF` | Cards and panels |
| Brand berry | `#8B3A62` | Primary accent |
| Soft rose | `#F4E8EE` | Subtle accent surfaces |
| Border | `#E9E3E6` | Dividers and outlines |
| Success | `#26734D` | Confirmed success |
| Warning | `#98651A` | Caution |
| Danger | `#B33B45` | Errors |

These are proposed documentation tokens. Reconcile them with the actual production CSS before applying global changes.

## <strong>03</strong> Typography and spacing

Use a legible sans-serif for controls and body copy. Reserve display typography for campaign headings. Use a 4px spacing base and consistent section rhythm.

| Token | Size | Typical use |
|---|---:|---|
| Space 1 | 4px | Icon gap |
| Space 2 | 8px | Compact control |
| Space 3 | 12px | Card internals |
| Space 4 | 16px | Standard gap |
| Space 6 | 24px | Section spacing |
| Space 8 | 32px | Large gap |
| Space 12 | 48px | Editorial separation |

## <strong>04</strong> Core component patterns

<div class="doc-grid two">
<div class="doc-card pink"><span class="ico">🧭</span><strong>Header and navigation</strong><p>Clear logo, search, account, wishlist and cart. Mobile menu must not overlap icons or content.</p></div>
<div class="doc-card purple"><span class="ico">🛍️</span><strong>Product cards</strong><p>Consistent image ratios, clear product name and price, restrained badges and useful hover states.</p></div>
<div class="doc-card blue"><span class="ico">🖼️</span><strong>Product detail</strong><p>Visual gallery, obvious variant selection, stock messaging and reachable add-to-cart action.</p></div>
<div class="doc-card green"><span class="ico">✍️</span><strong>Forms and feedback</strong><p>Persistent labels, inline validation, loading, disabled, success and error states.</p></div>
</div>

## <strong>05</strong> Responsive test matrix

| Viewport | Design expectation |
|---|---|
| 320px | No horizontal overflow; compact navigation |
| 360–390px | Comfortable touch targets and readable product details |
| 768px | Balanced tablet layouts |
| 1024px | Expanded catalogue and navigation |
| 1440px | Editorial whitespace and wider product grid |

## <strong>06</strong> Motion and interaction

- Keep transitions short and purposeful.
- Respect `prefers-reduced-motion`.
- Essential content must never depend on hover.
- Ensure focus and hover communicate equivalent affordances.
- Avoid layout shifts when images or content load.

## <strong>07</strong> Accessibility review

<div class="doc-grid">
<div class="doc-card green"><span class="ico">⌨️</span><strong>Keyboard</strong><p>All controls and menus are operable.</p></div>
<div class="doc-card blue"><span class="ico">👁️</span><strong>Contrast</strong><p>Text and controls remain legible.</p></div>
<div class="doc-card purple"><span class="ico">🏷️</span><strong>Labels</strong><p>Inputs and icon-only controls have names.</p></div>
<div class="doc-card yellow"><span class="ico">♿</span><strong>Motion</strong><p>Reduced-motion preferences are respected.</p></div>
</div>

<div class="doc-footer">UBHAYA FASHIONS · Design handbook · Review real mobile and desktop screenshots for every meaningful UI change.</div>
