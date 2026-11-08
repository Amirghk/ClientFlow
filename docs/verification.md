# Testing

ClientFlow combines domain tests, HTTP integration tests, frontend component tests, and browser tests. Run results are environment-specific; GitHub Actions workflow runs provide CI results.

## Backend

From the repository root:

```powershell
dotnet restore ClientFlow.slnx --locked-mode
dotnet test ClientFlow.slnx -c Release
```

The default suite uses SQLite for relational HTTP tests and exercises the real JWT bearer middleware with locally signed RS256 tokens. Test signing keys and issuer configuration exist only in the test project. Domain tests cover archival rules, conversion eligibility, invitation expiry and consumption, monetary validation, and administrator invariants. HTTP tests cover authentication failures, tenant boundaries, relationship ownership, CRUD workflows, concurrency conflicts, pagination, and reporting.

### PostgreSQL mode

```powershell
$env:CLIENTFLOW_TEST_POSTGRES = 'Host=localhost;Port=5432;Database=clientflow;Username=clientflow;Password=LOCAL_TEST_PASSWORD'
dotnet test ClientFlow.slnx -c Release
Remove-Item Env:CLIENTFLOW_TEST_POSTGRES
```

Use a test account with permission to create databases. The fixture creates a randomly named test database, applies migrations, and removes that database when finished. PostgreSQL-specific tests check conversion uniqueness and competing administrator/invitation updates. These tests are skipped when the connection variable is absent. SQLite success does not establish PostgreSQL compatibility.

## Frontend

```powershell
cd frontend
npm ci
npm test
npm run build
npx playwright install chromium
npm run test:e2e
```

Component tests cover form submission, error states, stale-edit reload, optimistic rollback, demo write rejection, and missing authentication configuration. Browser tests exercise the read-only demo, mobile navigation, search, and disabled mutations. Separate simulated-API browser tests cover forms, organization switching, pipeline interactions, and invitation errors.

The browser fixture is a test-only entry point and is excluded from the production build. Simulated-API tests are not full-stack tests and do not verify live Auth0 or PostgreSQL connectivity.

## Integration acceptance

Use a configured Auth0 tenant and PostgreSQL instance to verify Universal Login, callbacks/logout, access-token renewal, invitation return flows, and the complete application workflow. Follow the [manual acceptance checklist](review-checklist.md).

## Continuous integration

The backend workflow builds the solution and runs SQLite and PostgreSQL test modes. The frontend workflow checks the production build, component tests, and Chromium browser tests. Test artifacts are retained by the workflows for diagnosis.

## Container smoke tests

The Compose workflow builds the API, migration runner, and frontend images, then starts them with PostgreSQL and optional Adminer. It checks readiness, SPA deep links, unauthenticated API proxy responses, and repeat migration execution. Test Auth0 values in this workflow are syntax-valid placeholders; the smoke test does not verify Universal Login or remote Auth0 connectivity.

The default Compose configuration serves the production frontend build. `docker-compose.dev.yml` replaces the API/frontend processes with source watchers for local development. Both configurations share the database and migration dependency chain.
