# ClientFlow engineering conventions

## Architecture

- Keep the backend split into API, Common, Application, Infrastructure, and Domain projects.
- Controllers belong in the API's `Controllers` folder. They translate HTTP inputs/results and delegate use cases; never query EF or implement business rules.
- Application coordinates use cases through persistence and current-user interfaces. It must not depend on API, EF Core, or Infrastructure.
- Domain owns invariants and aggregate behavior. Keep domain entities independent of HTTP, persistence, authentication providers, and external I/O. Reference other aggregates by ID.
- Infrastructure implements Application interfaces and owns database mappings, migrations, health probes, and provider-specific failures.
- Common holds deliberately shared constants and primitives, not a collection of unrelated services.
- Each production project has `RegisterDependencies.cs`, exposing `RegisterAPI`, `RegisterCommon`, `RegisterApplication`, `RegisterInfrastructure`, or `RegisterDomain`. Program.cs is the composition root. Common/Domain registrations may remain empty while no injectable services are needed.
- Apply the context-sensitive modeling principles of Vlad Khononov's *Learning Domain-Driven Design*. Do not add domain events, generic repositories, messaging, or other patterns without a concrete use case.

## Configuration and persistence

- Map application configuration to typed settings classes, each with a `SectionName` constant. Bind and validate options at startup.
- Keep tenant-specific Auth0 values out of checked-in configuration; supply them through environment variables or local settings. Never introduce an authentication bypass.
- Define database index names in Common constants and use them in EF mappings and provider error handling. Treat constants used by historical migrations as immutable.
- Keep authenticated organization membership checks at server boundaries. Never trust caller-supplied roles or organization IDs as authorization.

## Style and tests

- Put a blank line before every explicit `return` statement in handwritten code.
- Separate distinct concepts within methods with blank lines; keep tightly related operations together. Use readable blocks for conditionals.
- Follow Vladimir Khorikov's *Unit Testing Principles, Practices, and Patterns*: test observable behavior, isolate domain rules from external I/O, and verify database-backed workflows with integration tests. Avoid mocking internal collaboration or asserting method-call choreography.
- Run relevant tests after changes. Do not report PostgreSQL, Auth0, or CI verification unless executed.
- Preserve existing API contracts unless a behavior change is requested, and update setup/architecture documentation when conventions change.
