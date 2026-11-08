# Verification — 2026-10-11

Executed on Windows using .NET SDK 10.0.300 / runtime 10.0.8:

- Package restore succeeded after pinning patched OpenAPI and SQLite native dependencies. NuGet vulnerability warnings are treated as errors.
- Release build/test execution succeeded: **28 passed, 0 failed, 0 skipped**.
- Tests exercised the real JWT bearer middleware with locally signed RS256 tokens, including wrong audience/issuer/signature, expiry, malformed/unsigned tokens, rejected machine identities, and missing subjects.
- HTTP tests covered anonymous rejection, organization creation/listing/selection/rename, outsider access denial, Member/Admin permissions, input validation, and pagination bounds.
- PostgreSQL model/migration consistency check passed without a live database.
- Release API publish succeeded.
- EF generated the PostgreSQL migration SQL successfully.
- Docker Compose configuration validation succeeded using a temporary non-secret placeholder password.

Not executed:

- PostgreSQL integration/migration application: Docker Desktop's Linux engine was not running.
- Docker image build/run: same limitation.
- Live Auth0 login/token verification: the user has no tenant yet and requested empty configuration.
- GitHub Actions: the workflow exists locally; the repository has not been pushed.

The HTTP tests used SQLite. The repository includes a PostgreSQL test mode and CI service configuration, but their presence is not a claim that those runs passed.

## Clean architecture refactor — 2026-10-11

- The refactored six-project solution (five production layers plus tests) built successfully during Release test execution.
- **44 tests passed, 0 failed, 0 skipped**: the existing API/authentication behavior, domain invariants, rejected mutations, options validation, dependency boundaries, and controller OpenAPI generation.
- EF generated `AlignDomainModel` with empty `Up` and `Down` operations. The model consistency test passed; no database schema alteration is required by the refactor.
- Release API publishing succeeded after the new project references and registrations were introduced.
- Auth0 configuration remains empty. No live Auth0, PostgreSQL, Docker image, or remote CI run is claimed by this update.
