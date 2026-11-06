<div align="center">

# Design System
### The visual language of UBHAYA FASHIONS

**Elegant fashion editorial meets a clear, modern shopping experience.**

</div>

---

## 01 · Design direction

The storefront should feel premium, warm and product-led. Clothing photography is the hero; interface decoration should support discovery rather than compete with it.

**Design principles**

- Editorial: generous imagery, calm layouts and confident typography.
- Clarity: price, size, colour, stock and calls to action are easy to scan.
- Consistency: shared spacing, controls, cards and feedback patterns.
- Mobile-first: prioritize the small-screen shopping journey.
- Restraint: use accent colour intentionally; avoid excessive gradients, shadows and borders.

## 02 · Visual tokens

Treat these as design-system starting points. Before introducing global tokens, reconcile them with the current production CSS so existing brand assets and established components are not unintentionally changed.

| Token | Suggested value | Use |
|---|---|---|
| Ink | `#242126` | Main text |
| Muted | `#77717A` | Secondary text |
| Canvas | `#FAF8F7` | Page background |
| Surface | `#FFFFFF` | Cards and panels |
| Brand berry | `#8B3A62` | Primary accent |
| Soft rose | `#F4E8EE` | Subtle accent backgrounds |
| Border | `#E9E3E6` | Dividers and control outlines |
| Success | `#26734D` | Confirmed success |
| Warning | `#98651A` | Caution |
| Danger | `#B33B45` | Errors and destructive actions |

## 03 · Typography

- Use a highly legible sans-serif for body copy, controls and product metadata.
- Use a restrained editorial display face for campaign headings only if it is already loaded reliably.
- Establish clear hierarchy: page title, section heading, product title, body and metadata.
- Avoid tiny low-contrast text for sizes, prices, delivery information or validation errors.
- Use sentence case for interface labels.

## 04 · Layout and spacing

Use a consistent spacing scale (4px base):

| Token | Size | Typical use |
|---|---:|---|
| `space-1` | 4px | Icon/text gap |
| `space-2` | 8px | Compact control spacing |
| `space-3` | 12px | Card internals |
| `space-4` | 16px | Standard component gap |
| `space-6` | 24px | Section spacing |
| `space-8` | 32px | Large section gap |
| `space-12` | 48px | Editorial separation |
| `space-16` | 64px | Desktop section rhythm |

Use a centered content container with a readable maximum width. Avoid fixed widths that create horizontal scrolling on mobile.

## 05 · Core components

### Header and navigation
- Keep logo, search, account, wishlist and cart discoverable.
- Use a clear mobile menu with a large enough touch target.
- Prevent icon collisions and wrapping at narrow widths.
- Make the logo a home link with an accessible name.

### Product cards
- Keep image ratios consistent within a collection.
- Use high-quality, appropriately sized images with explicit dimensions.
- Show product name and price with strong hierarchy.
- Show discount and availability without visual clutter.
- Hover effects must be subtle and must not hide essential information.
- On touch devices, never make key content dependent on hover.

### Product detail
- Make the gallery the visual focus.
- Keep selected size/colour state obvious.
- Communicate unavailable variants and stock clearly.
- Keep the add-to-cart action prominent and reachable on mobile.
- Avoid layout shifts when images load or variants change.

### Forms and feedback
- Persistent labels, clear required indicators and inline validation.
- Disabled, loading, success and error states.
- Confirm destructive actions.
- Success messages must follow server confirmation, not merely a button click.

## 06 · Responsive behaviour

| Viewport | Guidance |
|---|---|
| Small mobile | Single-column content, compact header, touch-friendly controls |
| Large mobile | Flexible product grid and uncluttered filter access |
| Tablet | Balanced two-column product layouts where appropriate |
| Desktop | Wider catalogue grid and editorial whitespace |

Test at 320px, 360px, 390px, 768px, 1024px and 1440px. These are test widths, not hard-coded breakpoints.

## 07 · Motion

- Keep transitions short and purposeful.
- Respect `prefers-reduced-motion`.
- Avoid motion that delays product selection or causes accidental interaction.
- Hover and focus should communicate the same essential affordances.
- Do not animate large layout regions on every pointer movement.

## 08 · Accessibility checklist

- [ ] Semantic headings and landmarks.
- [ ] Keyboard-operable menus, dialogs, filters and galleries.
- [ ] Visible focus indicators.
- [ ] Labels and accessible names for icon-only controls.
- [ ] Sufficient text and control contrast.
- [ ] Meaningful alternative text for product imagery.
- [ ] Errors announced and associated with relevant fields.
- [ ] Reduced-motion support.
- [ ] Touch targets comfortable on mobile.

## 09 · Design review

Review every meaningful UI change on a real narrow viewport and desktop. Check empty/loading/error states as well as the happy path. Product photography, not decorative UI, should remain the strongest visual element.
