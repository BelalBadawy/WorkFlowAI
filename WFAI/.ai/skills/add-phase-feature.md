---
name: Add Phase Feature
category: feature
description: Skill for adding a new Phase-related feature to the codebase.
triggers:
  - add phase feature
  - create phase
  - update phase
  - phase management
  - new phase page
  - phase API
---

# Add Phase Feature

## Overview
This skill provides a comprehensive guide for adding or extending a Phase-related feature within the **WFAI** codebase. The project follows a **Clean Architecture** pattern with **CQRS (Mediator)** and **EF Core** on the backend (.NET C#), paired with a **React (TypeScript)**, **TanStack Query**, and **Tailwind CSS** frontend.

Use this guide to ensure consistency across domain entities, EF Core configurations, Mediator CQRS handlers, validation, Minimal API endpoints, outbox events, cache invalidation, frontend API wrappers, and React Query custom hooks.

---

## Codebase Conventions

### File Structure
- **Domain Layer (`WFAI.Domain`)**: Defines domain entities (`Phase.cs`) implementing interfaces like `IFullEntity` and `IDataConcurrency`.
- **Infrastructure Layer (`WFAI.Infrastructure`)**: DbContext (`ApplicationDbContext`), Fluent API configurations (`PhaseConfiguration.cs`), and EF Core migrations.
- **Application Layer (`WFAI.Application`)**: CQRS features organized under `Features/Phases/`:
  - `Commands/`: Create, Update, Delete, RestorePhase, ChangePhaseStatus.
  - `Queries/`: GetPhasesPaged, GetPhaseById, ExportPhases.
  - `Events/`: Outbox events (`PhaseCreatedEvent`, etc.).
  - `PhaseCacheKeys.cs`: Centralized cache key management.
- **API Layer (`WFAI.API`)**: Minimal API endpoints in `Endpoints/PhaseEndpoints.cs` mapping route groups, handling authorization via `AppPermission`, and returning `IResponseWrapper<T>`.
- **Frontend Client (`WFAI.Client`)**:
  - API Client: `src/lib/phases-api.ts`
  - Custom Hooks: `src/hooks/usePhases.ts` (built with TanStack React Query)
  - Management Page: `src/pages/PhasesManagement.tsx`
- **Tests**: xUnit test projects matching layer names (`WFAI.Domain.Tests`, `WFAI.Application.Tests`, `WFAI.API.Tests`).

### Naming Conventions
- **Entities**: Singular PascalCase (`Phase`).
- **Commands & Queries**: `Verb + Entity + Command/Query` (e.g., `CreatePhaseCommand`, `GetPhasesPagedQuery`).
- **Validators**: `[Command/Query]Validator` (e.g., `CreatePhaseCommandValidator`).
- **DTOs / Requests**: `PhaseDto`, `CreatePhaseRequest`, `UpdatePhaseRequest`.
- **API Routes**: Lowercase kebab/plural `/api/v1/phases`.
- **Frontend Files**: `phases-api.ts`, `usePhases.ts`, `PhasesManagement.tsx`.

### API & Response Patterns
- Controllers use ASP.NET Core Minimal APIs via `.MapGroup("api/v{version:apiVersion}/phases")`.
- CQRS handlers return `IResponseWrapper<T>`.
- API Endpoints map response wrappers using `.ToApiResult()`.
- Endpoints require explicit permissions using `.RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Phases, AppAction.Create))`.

---

## Step-by-Step Feature Integration Guide

1. **Domain Entity (`WFAI.Domain/Entities/Phase.cs`)**
   - Implements `BaseEntity<int>`, `IFullEntity`, `IDataConcurrency`.
2. **Infrastructure Configuration (`WFAI.Infrastructure/Persistence/DbConfigurations/PhaseConfiguration.cs`)**
   - Configures table `"Phases"`, filtered unique index on `NormalizedTitle` where `SoftDeleted = 0`, and concurrency token `RowVersion`.
3. **Application Permissions (`WFAI.Application/Authorization/AppPermissions.cs`)**
   - Registers `AppFeature.Phases` permissions.
4. **CQRS Commands & Queries (`WFAI.Application/Features/Phases/`)**
   - Handles commands, queries, cache invalidations, and outbox messages.
5. **Minimal API (`WFAI.API/Endpoints/PhaseEndpoints.cs`)**
   - Exposes RESTful endpoints mapped into ASP.NET Core pipeline.
6. **Frontend Integration (`WFAI.Client/src/`)**
   - Provides API service wrapper, TanStack React Query hook, and React management page UI.
