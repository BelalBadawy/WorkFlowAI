# 04 — Phase Test Suite (Domain, CQRS & API Integration Tests)

**What to build:**
Write automated unit and integration tests verifying Phase domain rules, CQRS commands & queries, and API endpoints.

**Blocked by:** 02 — Phase CQRS Application Slice & Minimal API Endpoints

**Status:** completed

- [x] Write Domain unit tests for `Phase` entity in `WFAI.Domain.Tests`.
- [x] Write CQRS Command and Query handler unit tests in `WFAI.Application.Tests/Handlers/Phases/`.
- [x] Write Minimal API endpoint integration tests in `WFAI.API.Tests/Endpoints/PhaseEndpointsTests.cs`.
- [x] Execute `dotnet test` to confirm all tests pass cleanly.
