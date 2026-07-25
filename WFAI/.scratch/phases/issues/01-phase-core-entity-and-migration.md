# 01 — Phase Core Entity, EF Core Migration & Permission Definitions

**What to build:**
Define the `Phase` domain entity in `WFAI.Domain`, configure EF Core entity mappings and migration in `WFAI.Infrastructure`, and register security permissions in `WFAI.Application`.

**Blocked by:** None — can start immediately

**Status:** completed

- [x] Create `Phase.cs` entity in `WFAI.Domain/Entities` implementing `BaseEntity<int>`, `IFullEntity`, and `IDataConcurrency` with fields: `Title`, `NormalizedTitle`, `Description`, `IsActive`, `SortOrder`, `RowVersion`, and soft delete audit fields.
- [x] Add `AppFeature.Phases` and register `Create`, `Read`, `Update`, `Delete` permissions in `AppPermissions.cs`.
- [x] Add `PhaseConfiguration.cs` in `WFAI.Infrastructure` with filtered unique index on `NormalizedTitle` where `SoftDeleted = 0`, soft delete query filter, and concurrency token.
- [x] Add `DbSet<Phase> Phases` in `ApplicationDbContext.cs` and `IApplicationDbContext.cs`.
- [x] Add EF Core migration `AddPhaseEntity`.
