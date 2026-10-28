# Interview walkthrough: Full Stack .NET UI Developer

This guide maps the supplied role description to demonstrable code. It is a portfolio discussion aid, not a claim about the candidate's employment, certifications or past project experience. The role weights .NET/C# and frontend/UI/UX equally, with accessibility across all areas.

## A five-minute demonstration

1. Open the public home page. Search “oil” or “leak”; show categories/options and how the user's area carries into a request.
2. Choose a service before sign-in. Sign in as the demo customer and show that service selection is preserved.
3. Go through the three-step form. Submit an empty details step to show error summary, focus, field links and retained input. Review before sending.
4. Open My requests. Compare estimates/revisions, inspect an assigned job's next action and show completed two-way feedback/activity.
5. Explain contact verification/provider approval from the account and admin views; show the source and the current GitHub test run.

## Explain the implementation with evidence

| Role area | Code/evidence | Talking point |
| --- | --- | --- |
| C#/.NET | `backend/ServiceConnect.Api/Workflow.cs` | Business transitions, ownership/role checks, serializable transactions, stale revision handling and wrapped SQL deadlock conflicts |
| REST/middleware | `Program.cs`, `ExtraEndpoints.cs`, `frontend/src/api-client.ts` | Boundary validation, JWT policies, rate limits, typed transport adapter and user-facing errors |
| UI/UX | `app.html`, `ui/request-wizard.*`, `catalog.ts` | Category-first information architecture, progressive disclosure, review-before-submit and deliberate error recovery |
| HTML/CSS/Sass | `styles.scss`, `styles/_tokens.scss`, `styles/_mixins.scss` | Semantic token roles, reusable surfaces/focus, responsive layouts and reduced motion |
| Enterprise design systems | `DESIGN_SYSTEM.md`, reusable `ui/` components | A system includes usage rules, tokens, accessibility contracts, component states, governance and tests; a widget library alone is not a design system |
| Accessibility | `ACCESSIBILITY.md`, `e2e/accessibility.spec.ts` | Keyboard focus, name/role/value, field/error relationships, reflow and axe; explain the limits of automation and the remaining JAWS/manual audit |
| SQL Server | `Models.cs`, both migration folders, `SchemaUpgradeTests.cs` | Relational constraints, one estimate per provider/request, scoped service options, provider-specific migrations and preservation of existing data |
| Modernization | Catalog migration and upgrade test | Extend an existing application without erasing requests, rather than restarting from a new demo database |
| CI/CD | `.github/workflows/ci.yml`, `azure-pipelines.yml` | Build once, verify backend/SQL/UI/browser behavior, retain accessibility reports; Azure YAML is supplied but has not been run in an Azure organization |
| Security | PasswordHasher, JWT, HMAC OTPs, ownership checks | Hash passwords and one-time codes appropriately, protect secrets, minimize data exposure; the sample is not a classified-data handling environment |

## Patterns that are actually present

- **Composition / presentational components:** category cards, icons and status pills are independent reusable UI units; the request wizard owns its form interaction contract.
- **View-model/facade boundary:** AppComponent coordinates views and user actions, while ServiceApiClient contains HTTP transport and normalized errors. This is a small application's boundary, not a claim of a full enterprise state platform.
- **Dependency injection and adapter boundary:** the API injects AppDb and ICodeDelivery; local/external delivery and test capture adapters share a contract. Database providers are selected behind a scoped AppDb abstraction.
- **Explicit state-transition rules:** Workflow permits only the valid job transitions; optimistic tokens/revision numbers and transactions protect concurrent changes. This is not an inheritance-based GoF State implementation.
- **Unit of work:** EF DbContext tracks changes and commits them together; no redundant generic repository wrapper is added just to name a pattern.
- **Pure validation/presentation functions:** catalog search and draft validation can be tested independently of rendering/network access.
- **Progressive disclosure and review:** a UI interaction pattern used to reduce cognitive load and prevent accidental submission.

## Likely questions and honest answers

**“How does this differ from Bootstrap or Material?”**
Bootstrap/Material provide styles and widgets. The design-system layer governs semantic roles, approved usage, interaction states, accessibility evidence and change review. This sample uses custom Angular components and Sass, not either library. In the State's environment, I would consume its approved components and map tokens/theming rather than ship a separate theme. Do not claim Bootstrap/Material implementation experience based solely on this sample.

**“Does a green axe run prove WCAG 2.1 AA or Section 508?”**
No. It checks selected machine-testable rules in exercised states. Manual keyboard, zoom, screen-reader, timing and usability testing remain. Discuss JAWS results only after actually running JAWS; do not invent certification or formal training.

**“Why test both SQLite and SQL Server?”**
A local file makes setup easy, but providers differ. Separate migrations and SQL integration tests exercise the real server's constraints and locking. The upgrade test demonstrates that an existing request is retained when sub-services are added.

**“Where are MVC/Web Forms and Azure production deployments?”**
This sample uses ASP.NET Core APIs plus Angular. It does not implement legacy MVC/Web Forms, and no Azure/AWS production deployment is claimed. Explain genuine prior experience separately. The Azure pipeline is a runnable configuration needing an actual organization/service connection to verify.

## Portfolio evidence and gaps

Use the GitHub repository link and actual CI run in the portfolio. Pair a short customer-flow screenshot/video with the design-system and accessibility notes so reviewers see UI work, not only backend code. The supplied role's formal training, JAWS, enterprise support, classified/sensitive-data practices and legacy stack requirements need separate, truthful evidence from the candidate.
