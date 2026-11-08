# Backend security and tenancy

## Authentication

Auth0 owns credentials, Universal Login, and token issuance. The API validates user access tokens using RS256, issuer, audience, signature, and expiration checks. Machine identities and ID tokens are not accepted for user workflows. Health endpoints and development OpenAPI are explicitly public; business endpoints require authentication.

## Tenant isolation

Organization IDs select candidate resources, not trusted authorization context. Each business operation verifies membership using the authenticated subject and checks referenced records within the same organization. Composite foreign keys enforce tenant ownership in PostgreSQL. Caller-supplied role or profile fields never grant access.

Outsiders receive 404 for organization resources. Members may manage business records; administrative operations return 403 unless the caller is an Admin. Membership changes are protected by the organization aggregate and its concurrency token, including the last-administrator rule.

## Persistence and concurrency

Local identities use a unique external-subject index. Mutable records carry optimistic concurrency versions; stale updates return 409. Conversion, invitation acceptance, membership changes, and their activity records commit transactionally. Migrations are applied explicitly rather than during API startup.

Database index names are defined in Common constants. Configuration is mapped to typed settings classes with section-name constants and startup validation. Request logs use correlation IDs without recording tokens, invitation secrets, or request bodies.

See [architecture](mvp-architecture.md) for project boundaries, the [API contract](api.md) for operations, and the [testing guide](verification.md) for verification methods.
