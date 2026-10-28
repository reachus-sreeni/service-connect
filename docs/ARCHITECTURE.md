# Architecture and API

Angular uses relative `/api` URLs and an in-memory JWT. The ASP.NET Core API validates inputs and roles and uses one EF model with SQLite and SQL Server implementations. Local mode uses a filesystem SQLite database and private OTP outbox. Server mode uses SQL Server and configured SMTP/Twilio delivery. There are separate migration sets for the two providers. See SCHEMA.md and DATABASE.md.

## Endpoints

| Method | Path | Access / behavior |
| --- | --- | --- |
| GET | /health | Public process health |
| POST | /api/auth/register | Email, phone, name, password, Customer/Provider role; providers add businessName and qualifications |
| POST | /api/auth/login | Email/password; token, name and role |
| GET | /api/me | Own contact verification flags and provider profile |
| POST | /api/me/verification/{email or phone}/send | Authenticated; issue code to own contact; returns challengeId/expiry only |
| POST | /api/me/verification/{email or phone}/confirm | Own challengeId and six-digit code |
| GET | /api/services | Authenticated service catalog |
| GET | /api/admin/providers | Admin review queue/profile details |
| POST | /api/admin/providers/{id}/decision | Admin; approve boolean and required notes |
| GET | /api/requests | Own requests; providers see open plus assigned jobs; admins see all; latest 200 |
| POST | /api/requests | Verified Customer; title, description, location and serviceId |
| GET | /api/requests/{id}/estimates | Owner/Admin sees all; provider sees own estimate and revisions |
| PUT | /api/requests/{id}/estimate | Verified, Approved Provider; amount, notes, expectedRevision |
| POST | /api/requests/{id}/accept | Verified owner; estimateId and expectedRevision |
| POST | /api/requests/{id}/work-done | Accepted Provider; marks work ready for customer confirmation |
| POST | /api/requests/{id}/complete | Owner; overall, quality, communication, punctuality (all 1–5), feedback |
| POST | /api/requests/{id}/customer-review | Accepted Provider after completion; overall, communication, cooperation, readiness (all 1–5), feedback |
| GET | /api/requests/{id}/history | Owner, accepted Provider or Admin; activities and reviews |

No public endpoint creates administrators or returns password/code hashes. Registration cannot self-approve a provider. Provider eligibility is checked from current database state when quoting and accepting; tokens do not carry stale approval claims.

Serializable workflow transactions and concurrency tokens prevent inconsistent acceptance/completion/review updates. Wrapped SQL deadlocks and SQLite busy errors become 409 so the user refreshes rather than silently changing the reviewed action. Reviews and completion are atomic, and unique indexes protect quote/review multiplicity. Audit records are appended by the API and have no public update/delete endpoint.

JWT sessions expire after one hour and are not persisted in browser storage. Password reset, email/phone changes, refresh/revocation tokens, notifications beyond verification, payment processing and formal identity/background checks remain out of scope. Contact changes would need to clear verification and re-verify the new destination. HTTP throttling is per instance and remote IP; production proxies/distributed limits need deployment-specific configuration. Health reports process availability, not database readiness.
