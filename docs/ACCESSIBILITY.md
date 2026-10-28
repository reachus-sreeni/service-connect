# Accessibility implementation and test plan

The supplied role makes accessibility a core requirement. Service Connect targets WCAG 2.1 A/AA in design and automated tests. Passing axe is useful evidence for tested states, not a complete conformance claim or a Section 508 certification.

## Implemented patterns

- Semantic header/nav/main/footer landmarks, one page-level heading and a keyboard-visible skip link.
- Labelled inputs and radio fieldsets, autocomplete for contact details, and text instructions for required formats.
- Wizard error summary with links, field-level error text and aria-invalid; errors/step headings receive focus while draft values remain intact.
- Native buttons and links, visible focus, selected/current states exposed to assistive technology, and no required hover interaction.
- Explicit status text, responsive reflow, large action targets and reduced-motion support.
- Global alert/status messages for errors and action results. Requests and reviews remain protected by API role/ownership checks.
- In-memory sessions and a clear sign-in boundary; no inaccessible CAPTCHA or forced transcription beyond the verification code workflow.

## Automated browser checks

```sh
cd frontend
npm ci
npx playwright install chromium
npm run test:e2e
```

The runner starts isolated API/frontend processes on ports 5001/4201 and a separate `.e2e-data` SQLite file. It does not modify the database used by the normal localhost:4200 preview. Run from a shell with .NET 10 and Node 22.22.3+ available. Linux CI installs browser OS dependencies with `npx playwright install --with-deps chromium`.

`e2e/accessibility.spec.ts` tests public discovery, service options, search/sign-in intent, request review/submission, error focus, registration, provider estimates, completed feedback, administrator review and mobile reflow. axe checks use wcag2a, wcag2aa, wcag21a and wcag21aa tags; results are attached to the Playwright HTML report. The GitHub workflow uploads that report even after a failure.

The local managed Mac environment blocks Chromium's launch, so a local runner launch failure is not recorded as an accessibility pass. GitHub's Linux run is the authoritative automated-browser result for this environment. In-app browser visual/interaction checks are recorded separately.

## Manual checks before claiming conformance

| Check | Procedure | Current scope |
| --- | --- | --- |
| Keyboard | Tab/Shift+Tab from skip link through navigation, category selection, wizard, errors, estimates and account actions; confirm focus is visible and not trapped | Scripted subset plus in-app interaction review; complete manual audit still required |
| JAWS + Chrome/Edge on Windows | Read landmarks/headings, labels, required state, radio selection, errors, progress and live updates | Not run here; Windows/JAWS is unavailable |
| VoiceOver + Safari | Repeat primary customer/provider flows and verify reading order/status announcements | Requires a dedicated assistive-technology session; not claimed |
| Zoom/reflow | Test 200% text zoom and 400% browser zoom, 320 CSS-pixel width and text-spacing overrides | Mobile layout/overflow is automated; complete zoom/text-spacing audit remains |
| Contrast | Check normal, hover, focus, selected, invalid and disabled states; include non-text controls | axe covers rendered tested states; full state audit remains |
| Timing | Review one-hour session and ten-minute verification expiry, warning/recovery and preservation of a user's draft after re-authentication | Current session expiry signs out; robust timeout-warning/draft recovery is a known gap |
| Real user testing | Observe representative users performing discovery, quoting and review tasks | Not conducted; design decisions are heuristic, not claims of user-research outcomes |

Do not describe this sample as fully WCAG/Section 508 compliant until a complete audit and remediation have been performed. Formal WCAG training/certification and JAWS experience are candidate qualifications; this codebase cannot establish them.

Reference: https://www.w3.org/WAI/WCAG21/quickref/
