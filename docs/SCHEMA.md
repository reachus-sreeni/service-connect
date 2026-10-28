# Service Connect schema

Both SQLite and SQL Server use the same domain model and independent EF migration sets. GUIDs identify records, timestamps are UTC, and financial quote values are decimal(18,2) on SQL Server. Local SQLite stores decimal quote values without binary floating-point conversion. There are no payment or bank records.

| Table | Purpose and relationships |
| --- | --- |
| Accounts | Customer, Provider or Admin; unique normalized email and international phone; password hash, email/phone verification flags, concurrency version |
| Providers | One-to-one account profile; business name, qualifications, Pending/Approved/Rejected, last reviewer/time/notes and concurrency version |
| ProviderDecisions | Append-only administrator decisions; provider profile, administrator, status, notes and timestamp |
| Services | Unique service-category name/description, service family and featured-discovery flag; referenced by requests |
| ServiceOptions | Category foreign key, scoped option name, description and display order; unique (category, name) |
| Requests | Customer, service and optional service-option foreign keys; title, description, area, lifecycle status, accepted estimate ID, completion time and concurrency version |
| Estimates | Provider and request foreign keys; one row per (request, provider), current revision/amount/notes |
| Revisions | Estimate foreign key; unique (estimate, revision), immutable amount/notes/time history |
| Reviews | Request, reviewer and reviewee foreign keys; unique (request, direction); 1–5 scores, written feedback and timestamp |
| Activities | Request and actor foreign keys; append-only service event kind, details and time |
| VerificationChallenges | Account foreign key; email/phone channel, HMAC code hash, expiry, attempt count, consumed flag, concurrency version |

Table names correspond to DbSet names in the EF model. New requests require a service option. A composite foreign key ensures that option belongs to the selected category; older requests may retain a null option. The accepted estimate ID is assigned only by the transaction workflow, which validates request membership and revision. A direct FK from Requests to Estimates is intentionally omitted to avoid a circular insert dependency; requests must exist before estimates. All mutation endpoints validate this relationship.

## Service lifecycle

`Open → Accepted → AwaitingCustomerReview → Completed`

1. Verified customer posts a service request; RequestCreated activity is recorded.
2. Approved providers with verified contacts submit/revise one estimate each. Each revision is recorded and an activity is appended.
3. The customer accepts one reviewed revision. A serializable transaction validates ownership, provider eligibility and revision, records acceptance and freezes quoting.
4. The accepted provider marks work done. Other providers cannot update it.
5. The owning customer submits required overall, quality, communication and punctuality ratings plus feedback. The review and Completed status are saved atomically.
6. The accepted provider can submit one customer review: overall, communication, cooperation and appointment readiness, plus written feedback.

Completion always includes the customer's review through the application. Provider feedback may be submitted after completion; the service page shows whether it is still pending. Reviews cannot be edited or submitted twice. The database has check constraints for rating ranges, review direction/criteria, account role, provider/request state and positive quote amounts. Application transactions additionally enforce actor and lifecycle rules.

History/reviews are visible to the customer, accepted provider and administrator. Open request descriptions are available to registered providers; keep precise/private addresses out of public descriptions. Users see their own contact details; administrators can see provider contact details for review. Providers cannot read competing estimates or private service history.

## Sample dataset

Local seed creates six synthetic accounts (one administrator, two customers, three providers), 34 service categories (five in Popular), 227 service options, four requests across the lifecycle, competing/revised estimates and completed feedback. All demo contact flags/approvals are simulated. The seed runs only for an empty account store and never resets manual changes on restart. It is disabled for SQL Server and non-Development environments.
