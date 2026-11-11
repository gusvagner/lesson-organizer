# Design system

The look of the Lesson Organizer frontend. Every page uses these tokens through CSS variables and the Angular Material theme; no page uses raw color values.

**Direction.** Calm and practical: a teacher opens this between lessons. A light gray-blue page with white cards, one cobalt blue for actions, a deep navy dark mode, rounded shapes, and Inter for everything. The palette is inspired by the light and dark tokens of ballastlane.com (read from its stylesheet on 2026-11-10): cobalt `#2B50BA`, navy `#0B1220`, gray-blue `#F4F6FA`, orange `#FF7633`. Only colors are borrowed; no logo, imagery or copy. No other real sites were compared (UXKIN was not connected).

## Color

| Token (`--lo-…`) | Light | Dark | Role |
|---|---|---|---|
| `page` | `#F4F6FA` | `#0B1220` | Page background |
| `surface` | `#FFFFFF` | `#121B2E` | Cards, toolbar, dialogs, board columns' cards |
| `surface-sunken` | `#EBEFF6` | `#0F1728` | Board columns, table header |
| `border` | `#DCE2EC` | `#26324A` | Card and table borders, dividers |
| `text` | `#0B1220` | `#E8ECF4` | Body text, headings |
| `text-muted` | `#5B6475` | `#9AA4B8` | Secondary text, labels, dates |
| `primary` | `#2B50BA` | `#86A0EE` | Primary buttons, links, focus ring, active nav |
| `primary-soft` | `#E8EDFA` | `#1A2850` | Active nav background, selected chips |
| `accent` | `#FF7633` | `#FF8A50` | Rare highlight only: the logo mark's dot, drag placeholder outline |
| `status-to-prepare` | `#B45309` on `#FEF3C7` | `#FBBF24` on `#3A2A08` | "To prepare" badge and column dot |
| `status-ready` | `#2B50BA` on `#E8EDFA` | `#86A0EE` on `#1A2850` | "Ready" badge and column dot |
| `status-delivered` | `#15803D` on `#DCFCE7` | `#4ADE80` on `#0E2E1B` | "Delivered" badge and column dot |
| `error` | `#B42318` | `#F97066` | Errors, destructive actions |

Text pairs meet 4.5:1 (body) and 3:1 (large text). The Material theme is generated from `primary` (`ng generate @angular/material:theme-color`); `page`, `surface` and `text` override the matching Material system tokens so both agree.

**Light and dark.** `html { color-scheme: light dark }` follows the system. The toolbar sun/moon button sets `color-scheme` to `light` or `dark`, and the browser remembers it (`localStorage`, key `lo-theme`).

## Typography

Inter Variable for everything, bundled from npm (`@fontsource-variable/inter`), no CDN.

| Use | Size / line height | Weight |
|---|---|---|
| Page title (h1) | 28 / 36 | 650 |
| Section title (h2) | 20 / 28 | 600 |
| Card title | 16 / 24 | 600 |
| Body | 16 / 24 | 400 |
| Label, button | 14 / 20 | 500 |
| Caption (dates, counts) | 13 / 18 | 450 |

Tabular numbers (`font-variant-numeric: tabular-nums`) in dates and counts.

## Spacing and layout

4 px base: 4, 8, 12, 16, 24, 32, 48. Page padding 24 (16 on phones). Content max width 1200. Gaps between cards 12, between sections 32.

## Shape and depth

| Token | Value | Used for |
|---|---|---|
| `radius-sm` | 8 px | Badges, menu items |
| `radius-md` | 12 px | Inputs, selects, form fields (outline) |
| `radius-lg` | 16 px | Cards, board columns, dialogs, tables' container |
| `radius-full` | 999 px | Buttons, nav pills, theme toggle, status selects on cards |
| `shadow-card` | `0 1px 2px rgb(11 18 32 / 6%)` | Cards at rest |
| `shadow-raised` | `0 8px 24px rgb(11 18 32 / 14%)` | Card being dragged, menus |

Dark mode drops shadows to borders only.

## Components

- **Buttons.** Primary: filled `primary`, full radius. Secondary: outlined with `border`. Quiet: text in `primary`. Hover darkens 8%, focus shows a 2 px `primary` ring with 2 px offset, disabled at 38% opacity.
- **Inputs and selects.** Material outline appearance, `radius-md`, label above the value, error text in `error` under the field.
- **Toolbar.** `surface` with a bottom `border`, logo mark + "Lesson Organizer", nav as pills (active: `primary-soft` background, `primary` text), theme toggle, user menu. Phone: nav in a menu button.
- **Cards.** `surface`, 1 px `border`, `radius-lg`, `shadow-card`, padding 16.
- **Board.** Three columns ("To prepare", "Ready", "Delivered") on `surface-sunken`, each with a colored status dot, the count, and cards sorted by lesson date. A card shows title, student, date and a small status select (keyboard and screen-reader alternative to dragging). While dragging: the card gets `shadow-raised`, the drop slot is a dashed `accent` outline. Phone: columns stack.
- **Status badge.** `radius-sm`, status colors above.
- **Empty state.** Muted icon, one sentence, one action.
- **Snackbar.** Material default on `text` background.

## Icons and imagery

Material Symbols Rounded, bundled from npm (`material-symbols`), 20–24 px, weight 400. The logo mark is an original SVG: a rounded square in `primary` with an open book drawn in white and a small `accent` dot; the same SVG is the favicon. No photos or illustrations.
