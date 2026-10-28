# RevenuePilot AI starter pack

- backend/src/RevenuePilot.Domain/Churn/ChurnScorer.cs: rule-based churn scoring (port of the prototype's risk())
- backend/tests/RevenuePilot.Tests/ChurnScorerTests.cs: xUnit tests
- docs/schema.postgres.sql: first-release PostgreSQL schema

Run tests (needs the .NET 8 SDK): cd backend/tests/RevenuePilot.Tests && dotnet test
Status: not yet compiled or run by the author; run the tests before relying on any of it.
