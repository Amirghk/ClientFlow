# ClientFlow API

All business routes require a valid Auth0 user access token. Send `Authorization: Bearer <token>`. Organization-scoped operations verify the caller's database membership; outsiders see 404. Both Admin and Member manage business data. Administrative operations require Admin and return 403 for a Member. Profile fields are never authorization inputs.

## Response and update conventions

Lists of organizations and business records return `{ items, page, pageSize, total }`. Page defaults to 1; pageSize defaults to 20. Allowed ranges are page 1–100000, pageSize 1–100. Business list query parameters:

| Parameter | Meaning |
| --- | --- |
| `search` | At most 160 characters; case-insensitive text search |
| `archived` | `false` by default; `true` returns archived records |
| `sort` | `name`, `nameDesc`, or `newest`; stable ID tiebreaker |
| `companyId` | Contacts, opportunities, projects |
| `projectId` | Tasks |
| `assignedToUserId` | Tasks |
| `status` | Opportunity stage, project status, or task status |

Mutable records include `version` (GUID). Send the latest value in every update, archive/restore, stage change, conversion, role change, member removal, profile update, or invitation revoke request. Membership changes use the **organization's version**, not a member version. Organization rename now also requires its version. Creation does not require a version. A stale version returns 409; reload before retrying. Competing conversions may return 409; a retry returns the already-created project.

Errors use `application/problem+json`, with status/title/detail and a trace ID for handled exceptions. Validation may include `errors`. Codes: 400 invalid input/business rule, 401 authentication, 403 admin permission, 404 inaccessible/missing record, 409 stale/concurrent relationship change, 429 rate limit, 500 unexpected failure with generic detail. Every response has `X-Correlation-ID`. Default rate limit is 120 requests per authenticated identity per minute, configurable in `RateLimiting`.

Dates use `YYYY-MM-DD`; timestamps are UTC ISO 8601. Amounts are non-negative decimals with at most two fractional digits, maximum 999999999999.99. Currency is an explicit three-letter uppercase code. Enum strings are case-sensitive.

## Identity and organizations

| Method | Route | Result |
| --- | --- | --- |
| GET | `/api/me` | Verified subject and nullable local user ID (original route) |
| POST | `/api/me` | Idempotently provision local identity; return profile |
| PUT | `/api/me` | `{ displayName, version }`, display name 1–100 characters |
| GET | `/api/organizations` | Paged memberships |
| POST | `/api/organizations` | `{ name }`, 1–160 characters; creator becomes Admin |
| GET | `/api/organizations/{id}` | Organization, caller role, and version |
| PUT | `/api/organizations/{id}` | Admin rename `{ name, version }` |

## CRM, sales and projects

In this table `{root}` means `/api/organizations/{organizationId}`. For `companies`, `contacts`, `opportunities`, and `projects`: GET `{root}/{resource}` lists, GET `{root}/{resource}/{id}` loads details, POST `{root}/{resource}` creates, PUT `{root}/{resource}/{id}` edits. POST `{root}/{resource}/{id}/archive` and `/restore` take `{ version }` and return the changed record. There is no business-record hard delete.

| Resource | Create/update fields |
| --- | --- |
| companies | `name`, `industry?`, `website?` (HTTP/HTTPS), `email?`, `phone?`, `version` for edits |
| contacts | `companyId`, `firstName`, `lastName`, `email?`, `phone?`, `jobTitle?`, `version` for edits |
| opportunities | `companyId`, `contactId?`, `name`, `value`, `currency`, `expectedCloseDate?`, `version` for edits |
| projects | `companyId`, `name`, `description?`, `status`, `startDate?`, `dueDate?`, `version` for edits |

Contacts require first/last names of 1–80 characters. Contact company cannot change after creation: archive/recreate to preserve relationship history. A selected opportunity contact must belong to the selected company in the same organization. Archived company/contact selections cannot create new relationships. Existing references remain readable, and archives never cascade silently.

POST `{root}/opportunities/{id}/stage` takes `{ stage, version }`. Stages: `New`, `Qualified`, `Proposal`, `Negotiation`, `Won`, `Lost`. All transitions, including reopening, are allowed for active records.

POST `{root}/opportunities/{id}/convert` takes `{ version }`. An active Won opportunity with an active company creates a Planned project and an activity record in one transaction. A unique organization/source-opportunity database index prevents duplicates; later requests return the same project. Subsequent opportunity changes do not modify its project.

Project statuses: `Planned`, `Active`, `OnHold`, `Completed`, `Cancelled`. Due date cannot precede start date. GET project details returns `{ project, totalTasks, completedTasks, progress }`; progress is a rounded integer percentage based on non-archived tasks, zero if empty.

## Tasks

| Method | Route |
| --- | --- |
| GET | `{root}/tasks` |
| GET | `{root}/tasks/{id}` |
| POST | `{root}/projects/{projectId}/tasks` |
| PUT | `{root}/projects/{projectId}/tasks/{id}` |
| POST | `{root}/tasks/{id}/archive` |
| POST | `{root}/tasks/{id}/restore` |

Create/update fields: `title`, `description?`, `assignedToUserId?`, `status`, `priority`, `dueDate?`, and `version` on edits. Statuses: `ToDo`, `InProgress`, `Done`. Priorities: `Low`, `Medium`, `High`, `Urgent`. Assignee must be a current organization member. New tasks require an active project. Restore requires an active parent. Removal of a member clears their assignments transactionally, including archived task assignments that would otherwise prevent removal.

## Administration and invitations

| Method | Route | Input/result |
| --- | --- | --- |
| GET | `{root}/members` | Array `{ userId, displayName, role }`; both roles can read |
| PUT | `{root}/members/{userId}/role` | Admin; `{ role: "Admin" or "Member", version: organizationVersion }` |
| POST | `{root}/members/{userId}/remove` | Admin; `{ version: organizationVersion }` |
| GET | `{root}/invitations` | Admin; latest 100, no token/hash |
| POST | `{root}/invitations` | Admin; returns `{ id, token, expiresAt }` once |
| POST | `{root}/invitations/{id}/revoke` | Admin; `{ version: invitationVersion }` |
| POST | `/api/invitations/accept` | Authenticated; `{ token }`; returns `{ organizationId }` |

Invitation tokens are cryptographically random 32-byte hex strings; only SHA-256 hashes persist. Links expire after seven days, are single-use and revocable, and grant Member access. Possession confers eligibility; there is no email binding. Consuming the token, adding membership, updating the organization version, and recording activity commit together. Existing members cannot consume a link again. The last administrator cannot be removed/demoted; the organization concurrency token protects competing changes.

## Reports and operations

GET `{root}/dashboard` returns `companies`, `activeOpportunities`, `activeProjects`, `overdueTasks`, `values` (each `{ currency, openValue, wonValue }`), `stages` (all six counts), and the latest 15 `activity` entries. Open excludes Won/Lost; overdue excludes Done/archived tasks and archived/Completed/Cancelled projects. No exchange-rate conversion occurs. Activity retains actor display-name snapshots after membership removal.

GET `/health/live` and `/health/ready` are public. Readiness checks persistence. Development exposes `/openapi/v1.json`. Telemetry is optional; no tokens or invitation secrets are added to application request logs.
