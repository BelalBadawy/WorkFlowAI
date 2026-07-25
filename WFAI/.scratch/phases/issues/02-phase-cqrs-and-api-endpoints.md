# 02 — Phase CQRS Application Slice & Minimal API Endpoints

**What to build:**
Implement all Phase CQRS commands, queries, validators, outbox events, and Minimal API endpoints under `api/v1/phases`.

**Blocked by:** 01 — Phase Core Entity, EF Core Migration & Permission Definitions

**Status:** completed

- [x] Create `PhaseDto`, `PhaseCacheKeys`, and Outbox events (`PhaseCreatedEvent`, `PhaseUpdatedEvent`, `PhaseDeletedEvent`).
- [x] Create `CreatePhaseCommand` & validator/handler.
- [x] Create `UpdatePhaseCommand` & validator/handler.
- [x] Create `DeletePhaseCommand`, `RestorePhaseCommand`, `ChangePhaseStatusCommand` & handlers.
- [x] Create `GetPhasesPagedQuery`, `GetPhaseByIdQuery`, `ExportPhasesQuery` & handlers.
- [x] Create `PhaseEndpoints.cs` mapping all endpoints to `api/v1/phases` with appropriate permission authorization.
- [x] Register `PhaseEndpoints` in `Program.cs`.
