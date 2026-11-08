# Review checklist and verification record

## Executed locally — 2026-10-15

| Check | Result |
| --- | --- |
| .NET Release build / HTTP and domain tests | Passed: 64 tests |
| PostgreSQL-only tests | 2 skipped; no configured engine |
| API Release publish | Passed |
| PostgreSQL idempotent migration SQL generation | Passed from Release build; includes CompleteWorkspace |
| EF model / migration snapshot consistency | Passed in backend tests |
| TypeScript check and Vite production build | Passed |
| Frontend component tests | Passed: 5 |
| Chromium Playwright tests | Passed: 5 |
| Desktop dashboard/settings and mobile table visual inspection | Completed; mobile page overflow fixed |
| npm dependency installation audit | 0 reported vulnerabilities at installation |

Backend tests exercise the real controllers, JWT bearer middleware and relational persistence using SQLite. They cover organization/profile updates, membership boundaries, references across tenants, contact/company consistency, archive/restore, conversion idempotence, projects/tasks/progress, invitations, last-admin rules, version conflicts, search/paging, reporting currencies, token failures and architecture boundaries. PostgreSQL-specific tests verify competing administrator/invitation snapshots and source-project uniqueness; the PostgreSQL test mode runs migrations and the same HTTP suite.

Browser tests cover actual demo navigation/read-only controls/search, mobile navigation/overflow, and **simulated API** company forms, organization switching, drag-and-drop and stage selection, and expired invitation handling. Component tests cover form success/failure, stale-edit reload, optimistic rollback, demo write rejection, and missing Auth0 setup. These tests do not establish live Auth0 or full-stack browser connectivity.

## Connection-dependent checks not executed

- PostgreSQL migration application, provider-specific constraints and concurrency tests. Docker's local database engine was not running; no PostgreSQL test connection was provided.
- Auth0 Universal Login, real issuer discovery/JWKS, user access-token acquisition/renewal, callback/logout, and invitation return flow with your tenant.
- Full-stack browser workflow using your PostgreSQL/Auth0 configuration.
- GitHub Actions execution and any hosted deployment.

The API and frontend use real integration interfaces with blank Auth0 configuration. No live connection or cloud deployment is claimed.

## Your review path

1. Start the frontend and visit `/demo`. Review overview, companies/contacts, Pipeline board/table, project details/tasks and Settings on desktop/mobile. All mutations must remain disabled.
2. Configure PostgreSQL and Auth0 using the README. Apply migrations explicitly, then run PostgreSQL tests with `CLIENTFLOW_TEST_POSTGRES` using a database-creation-capable test account.
3. Sign in, create an organization, and edit your display name. Create a company/contact, an opportunity, move it to Won and convert it. Repeated conversion must open the same project.
4. Create a standalone project; assign tasks, change status/priority/due dates, and verify progress/overdue totals. Add different currencies and confirm the report keeps totals separate.
5. Archive/restore each resource. Archived parents must block new relationships. Historical children remain visible. An archived task must not affect active progress.
6. Invite a second user. Verify Member access, link reuse/revoke/expiry, role changes, assignment clearing on removal, and last-administrator protection.
7. Use two browser tabs to make competing edits. The stale tab should receive a reloadable conflict without overwriting the newer changes. Verify pipeline failure rollback.
8. Switch organizations and sign out/in with another user. Confirm no prior organization's results appear. Verify direct URLs and foreign IDs cannot cross tenant boundaries.
9. Stop the API or expire a session. Confirm the UI explains the failure and never substitutes demo data. Verify correlation IDs and optional telemetry with your collector if enabled.

See [API contract](api.md) for enum values, versions, pagination and explicit operations, and [architecture](mvp-architecture.md) for modeling decisions. A contact's company is intentionally fixed after creation; its other fields remain editable. Invitation history is capped at the latest 100; the board represents its current paginated result.
