# Architecture and API

Angular uses relative `/api` URLs, an in-memory bearer token, form validation and explicit refresh controls. Development uses an Angular proxy; production uses CloudFront to route `/api/*` to the API and everything else to the static frontend. No credentials are stored in browser local storage.

The API validates inputs, authenticates JWTs, enforces Customer/Provider roles, and uses EF Core SQL Server. It stores Accounts, Requests, Estimates and EstimateRevisions. Unique indexes cover normalized email, (request, provider), and (estimate, revision). Password hashes never appear in responses. Each request holds one nullable accepted estimate ID. A transaction loads and checks the request before recording an estimate or acceptance. Revision numbers and a request concurrency token detect stale actions. SQL deadlocks/concurrent changes return 409; the user refreshes to see current data.

## Endpoints

| Method | Path | Access and result |
| --- | --- | --- |
| GET | /health | Public process health |
| POST | /api/auth/register | `{email,name,password,role}`; role Customer or Provider; returns token/name/role |
| POST | /api/auth/login | `{email,password}`; returns token/name/role |
| GET | /api/requests | Customer's requests; providers see all; newest 200 |
| POST | /api/requests | Customer; `{title,description,location}` |
| GET | /api/requests/{id}/estimates | Owner sees all; provider sees own estimate with history |
| PUT | /api/requests/{id}/estimate | Provider; `{amount,notes,expectedRevision}`; 0 creates, current revision updates |
| POST | /api/requests/{id}/accept | Owner; `{estimateId,expectedRevision}`; 204 success |

Amounts are positive USD decimals with at most two fractional digits. Quotes record scope/terms in notes. Acceptance of a stale revision, second acceptance, or a quote after acceptance returns 409. Missing objects return 404; wrong role/owner returns 403; unauthenticated access returns 401; invalid inputs return 400. A request accepted from one provider closes all further quoting.

## Operational limitations

No email verification, password reset, session revocation, refresh tokens or cross-instance rate-limit store. Authentication throttling is per instance and remote IP; behind a proxy it can group users under the proxy IP. Configure trusted forwarded headers and distributed limiting before scaling. Health checks do not verify SQL availability. Add monitoring for database failures and application errors. Bootstrap schema creation uses EnsureCreated; use migrations for future schema evolution. SQLite tests cover behavior but SQL Server tests are necessary for provider-specific behavior.
