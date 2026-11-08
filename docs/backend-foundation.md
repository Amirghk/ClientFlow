> Historical foundation notes. For the completed MVP and current scope, see [MVP architecture](mvp-architecture.md) and [README](../README.md).

# Backend foundation decisions

This is the user-authorized first backend increment from the ClientFlow plan, combining the required repository setup with basic authentication and tenancy. It does not mark every item in the plan's phases 1 or 2 complete. The direct request to build the backend supersedes the attachment's instruction to stop after planning.

## Boundaries

- Use .NET 10, EF Core, PostgreSQL, and a small modular monolith with API, Common, Application, Infrastructure, and Domain projects. Domain entities have no ASP.NET or EF dependencies.
- Auth0 owns credentials, registration, login, and token issuance. The API is a resource server, not a login server.
- ClientFlow owns organizations and memberships. No paid Auth0 organization feature is required.
- All API routes require a validated user identity by default. Health checks and development OpenAPI are explicitly public.
- Signature, RS256 algorithm, issuer, audience, and expiration are checked by the JWT bearer middleware; 30 seconds of clock skew is permitted. Test signing keys exist only in the test project.
- Organization IDs in routes select a candidate organization, not trusted tenant context. Every organization read/write starts from memberships filtered by the verified subject. No caller-provided role or organization claim grants access.
- Outsiders see 404; members attempting an admin operation see 403. Future CRM relationship lookups must validate both the organization membership and the referenced record's organization.
- Organization creation commits the organization and initial Admin membership in one transaction. Local identity provisioning uses a unique external-subject index and recovers from the concurrent-first-request race on PostgreSQL.
- Renaming is last-write-wins in this small increment. Introduce optimistic concurrency where competing business edits matter. Membership mutation is not exposed, so last-admin removal cannot occur through the API.
- PostgreSQL migrations are applied explicitly, separately from API startup. A database with no application schema is not ready.
- SQLite is a fast relational HTTP-test backend only, never an application fallback. CI is configured to rerun tests against PostgreSQL with migrations.

## Permission matrix

| Action | Anonymous | Authenticated non-member | Member | Admin |
| --- | --- | --- | --- | --- |
| Health | Yes | Yes | Yes | Yes |
| Create organization | No | Yes | Yes | Yes |
| Read selected organization | No | No | Yes | Yes |
| Rename selected organization | No | No | No | Yes |

Memberships are created for the organization owner only. Invitations, joining, membership lists, role changes, and removals require a later explicit workflow; there is no arbitrary self-join or subject-assignment endpoint.

## Deferred decisions and work

- Profile display names and verified emails, invitation flow and expiry, member removal, and last-administrator protection.
- Frontend Universal Login integration, organization switcher UI, and logout behavior.
- CRM, sales, project modules and tenant-owned foreign key rules.
- Audit/activity history, OpenTelemetry, distributed rate limits, production proxy/HTTPS configuration, secret provisioning, and deployment.
- Full PostgreSQL run and live Auth0 smoke test in the user's configured environment.

## Clean architecture refactor

The HTTP surface is implemented by `IdentityController`, `OrganizationsController`, and `HealthController`. Controllers call Application use cases; they do not reference DbContext or perform database queries. Application depends on Domain and Common, and defines interfaces implemented by Infrastructure. The API references Infrastructure only at the composition root.

Organization is the aggregate root for its memberships. Constructing it establishes an administrator, and its rename/add-member operations enforce administrator permissions. Memberships cannot be constructed or mutated by Application callers independently of the aggregate. Users are separate aggregates referenced by ID. Organization names are validated through an immutable value object; the EF mapping keeps the existing string column. Adding members is a domain operation for future use cases and test setup, not a newly exposed API capability.

Reads remain simple projections where appropriate. Repositories keep tenant filtering internal and do not expose IQueryable through Application interfaces. The aggregate is loaded with memberships for operations requiring its rules. No generic repository or message bus is introduced.

Auth0, CORS, rate-limit, and database configuration have typed settings classes with section-name constants and startup validation. Index names live in Common and are used consistently by mappings, migrations, and uniqueness-error handling. These constants must retain their values once referenced by a historical migration.

The model alignment migration has no schema operations. Existing database rows and endpoint contracts are retained. Common and Domain have explicit registration extension files as requested; their methods are empty until there is an actual service to register. DI abstractions are limited to these bootstrap files, keeping entity behavior independent of service containers.

Domain unit tests cover permissions and invariants using real objects. HTTP integration tests retain the real JWT middleware and relational persistence, rather than mocking internal layers. Architecture tests guard inward dependency direction. These choices follow the emphasis on isolated domain behavior and observable outcomes in [Khorikov's domain-isolation guidance](https://khorikov.org/5-unit-testing-mistakes-in-ddd/1-not-isolating-your-domain-model/) and [database-testing guidance](https://enterprisecraftsmanship.com/posts/how-to-assert-database-state/).

The initial local verification uses signed JWTs and SQLite to prove the HTTP authorization behavior. It does not contact Auth0 or establish that a production deployment is configured correctly.

