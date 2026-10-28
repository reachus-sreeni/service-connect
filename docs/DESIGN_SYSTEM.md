# Service Connect design system

This portfolio system combines tokens, reusable components, interaction patterns, accessibility rules, usage guidance and verification. A component library supplies widgets; a design system also explains how and when to use them and how changes are reviewed. This is an application-level system, not a claim to implement an existing government enterprise system.

## Foundations

`frontend/src/styles/_tokens.scss` defines semantic color roles, spacing, radii, shadows and content widths. `_mixins.scss` contains the shared focus and surface rules; `styles.scss` implements the responsive layouts. Components consume semantic roles such as `--sc-brand`, `--sc-ink`, `--sc-danger` and `--sc-focus` rather than choosing colors independently. System fonts avoid a third-party font dependency.

| Foundation | Rule |
| --- | --- |
| Typography | One readable system-font family, clear heading hierarchy, restrained display type |
| Layout | Content width capped at 1200px; desktop grids reflow into stacked mobile content |
| Spacing | Named spacing tokens and shared component padding/radius roles |
| Color | Purple actions, neutral surfaces, explicit success/error text; status is never color alone |
| Focus | Shared visible 3px focus ring; skip link and heading focus after navigation |
| Targets | Primary controls have at least 44px height; no hover-only essential controls |
| Motion | Short decorative transitions; prefers-reduced-motion disables them |

## Components and contracts

| Component | Responsibility | Accessibility behavior |
| --- | --- | --- |
| `ui/icon.ts` | Consistent code-native SVG icon vocabulary | Decorative icons hidden from assistive technology; controls carry text/labels |
| `ui/category-card.ts` | Category discovery and selected state | Native button, descriptive name, aria-pressed |
| `ui/status-pill.ts` | One shared status-to-label mapping | Explicit text alongside semantic color |
| `ui/request-wizard.ts/html` | Three-step request composition and validation | Fieldsets, radio labels, error summary links, focus on errors/step headings, aria-current step |
| App shell | Role-based navigation, account/workspace views | Main navigation landmark, page title, skip link, alert/status messages |
| Request workspace | Search/filter, estimate comparison, next action, history | Labelled filters, descriptive request controls, disclosure summaries and plain button groups |

The UI uses Angular standalone components, signals, HTML5 and Sass. It does not currently include Bootstrap or Angular Material. The implementation demonstrates the design-system layer that can govern either library: map library theming APIs to these semantic tokens and wrap widgets behind application components rather than mixing unrelated styles. A production enterprise rollout would adopt the organization's approved libraries and tokens, not introduce this portfolio theme unchanged.

## Interaction patterns

- **Discover before signing in:** the public catalog exposes no account data. Search matches category names, descriptions and service options.
- **Progressive disclosure:** choose a category, inspect service options, then request estimates. Technical verification and review controls stay in the account/workspace views.
- **Guided form:** service choice → details → review/send. Validation preserves entered data and links errors to the fields. An option must belong to its category, enforced by the UI, API and composite database foreign key.
- **Resume intent:** choosing a service before sign-in preserves category/option choice through authentication. Sensitive account data and tokens are not written to browser storage.
- **Clear next action:** job status controls whether to quote, mark done, confirm completion or submit feedback. Server-side authorization/state checks remain authoritative.
- **Honest estimates:** no invented fixed prices, availability, provider ratings or booking counts. Service Connect uses provider quotes and no payments.
- **Modernize without erasing data:** categories retain stable IDs, old custom requests can retain a null option, and migrations preserve existing rows. Electrical remains available under Other services for compatibility.

## Reference study

Urban Company's homepage and plumbing-service page informed category-first discovery, service-option groups, location context and the visible request summary. Service Connect has its own typography, tokens, illustrations and wording; it does not copy branding, photos, service prices or marketing claims. Gardening and Automotive Service are user-requested catalog additions, not claims about Urban Company's current offerings.

- https://www.urbancompany.com/
- https://www.urbancompany.com/bangalore-plumbers-density

## Change governance

For each component change: explain the user problem; reuse/add a semantic token; define default, hover, focus, selected, invalid, disabled and loading behavior; check keyboard order, names and error recovery; check desktop/mobile layouts and reduced motion; run component, browser and axe tests; document evidence and remaining manual checks. Breaking API/schema changes need migrations and compatibility tests for both database providers.
