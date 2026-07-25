---
name: Add Category Feature
category: feature
description: Skill for adding a new Category-related feature to the codebase.
triggers:
  - add category feature
  - create category
  - update category
  - category management
  - new category page
  - category API
---

# Add Category Feature

## Overview
This skill provides a comprehensive guide for adding or extending a Category-related feature within the **WFAI** codebase. The project follows a **Clean Architecture** pattern with **CQRS (Mediator)** and **EF Core** on the backend (.NET C#), paired with a **React (TypeScript)**, **TanStack Query**, and **Tailwind CSS** frontend.

Use this guide to ensure consistency across domain entities, EF Core configurations, Mediator CQRS handlers, validation, Minimal API endpoints, outbox events, cache invalidation, frontend API wrappers, and React Query custom hooks.

---

## When to Use
Use this skill whenever you need to:
- Add a new field or relationship to the `Category` entity (e.g. icon, description, parent category).
- Create a new Category API endpoint or CQRS command/query.
- Implement new Category business logic (e.g. status changes, bulk export, tree structure logic).
- Extend Category frontend management pages, dialogs, forms, or hooks.
- Write unit or integration tests for Category operations.

---

## Codebase Conventions

### File Structure
- **Domain Layer (`WFAI.Domain`)**: Defines domain entities (`Category.cs`) implementing interfaces like `IFullEntity` and `IDataConcurrency`.
- **Infrastructure Layer (`WFAI.Infrastructure`)**: DbContext (`ApplicationDbContext`), Fluent API configurations (`CategoryConfiguration.cs`), EF Core migrations, and infrastructure services (`CategoryExportService.cs`).
- **Application Layer (`WFAI.Application`)**: CQRS features organized under `Features/Categories/`:
  - `Commands/`: Create, Update, Delete, RestoreCategory, ChangeCategoryStatus.
  - `Queries/`: GetCategoriesPaged, GetAllCategories, GetCategoryById, ExportCategories.
  - `Events/`: Domain/outbox events like `CategoryCreatedEvent`.
  - `CategoryCacheKeys.cs`: Centralized cache key management.
- **API Layer (`WFAI.API`)**: Minimal API endpoints in `Endpoints/CategoryEndpoints.cs` mapping route groups, handling authorization via `AppPermission`, and returning `IResponseWrapper<T>`.
- **Frontend Client (`WFAI.Client`)**:
  - API Client: `src/lib/categories-api.ts`
  - Custom Hooks: `src/hooks/useCategories.ts` (built with TanStack React Query)
  - Management Page: `src/pages/CategoriesManagement.tsx`
- **Tests**: xUnit test projects matching layer names (`WFAI.Domain.Tests`, `WFAI.Application.Tests`, `WFAI.Infrastructure.Tests`).

### Naming Conventions
- **Entities**: Singular PascalCase (`Category`).
- **Commands & Queries**: `Verb + Entity + Command/Query` (e.g., `CreateCategoryCommand`, `GetCategoriesPagedQuery`).
- **Validators**: `[Command/Query]Validator` (e.g., `CreateCategoryCommandValidator`).
- **DTOs / Requests**: `[Action]CategoryRequest` or `Category[Type]Dto`.
- **API Routes**: Lowercase kebab/plural `/api/v1/categories`.
- **Frontend Files**: `categories-api.ts`, `useCategories.ts`, `CategoriesManagement.tsx`.

### API & Response Patterns
- Controllers use ASP.NET Core Minimal APIs via `.MapGroup("api/v{version:apiVersion}/categories")`.
- CQRS handlers return `IResponseWrapper<T>` (e.g. `ResponseWrapper<T>.Success(...)` or `ResponseWrapper<T>.Fail(...)`).
- API Endpoints map response wrappers using `.ToApiResult()`.
- Endpoints require explicit permissions using `.RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Create))`.

### Database Patterns
- EF Core Entity Configurations implement `IEntityTypeConfiguration<Category>`.
- Soft Delete: Filtered unique database indexes (`[SoftDeleted] = 0`).
- Concurrency Control: `byte[] RowVersion` is decorated with `.IsConcurrencyToken()`.
- Outbox Pattern: Mutation operations publish events via `_applicationDbContext.AddOutboxMessage(new CategoryCreatedEvent(category.Id))`.

### Frontend Patterns
- **API Service**: `categories-api.ts` exports an API client object using `api.get`, `api.post`, `api.put`, `api.delete`.
- **Hooks**: Custom React Query hooks in `useCategories.ts` handle caching, invalidating `['categories']` queries, optimistic updates, and toast notifications.

---

## Files to Modify or Create

When adding or extending a Category feature, the following files are involved:

- `WFAI.Domain/Entities/Category.cs`
- `WFAI.Infrastructure/Persistence/DbConfigurations/CategoryConfiguration.cs`
- `WFAI.Infrastructure/Migrations/<Timestamp>_<MigrationName>.cs`
- `WFAI.Application/Features/Categories/CategoryCacheKeys.cs`
- `WFAI.Application/Features/Categories/Commands/<Feature>/<CommandName>.cs`
- `WFAI.Application/Features/Categories/Commands/<Feature>/<CommandName>Validator.cs`
- `WFAI.Application/Features/Categories/Events/<EventName>.cs`
- `WFAI.Application/Features/Categories/Queries/<Feature>/<QueryName>.cs`
- `WFAI.API/Endpoints/CategoryEndpoints.cs`
- `WFAI.Client/src/lib/categories-api.ts`
- `WFAI.Client/src/hooks/useCategories.ts`
- `WFAI.Client/src/pages/CategoriesManagement.tsx`
- `WFAI.Application.Tests/Handlers/Categories/CategoryCommandHandlerTests.cs`

---

## Step-by-Step Implementation

1. **Update Domain Entity (`WFAI.Domain/Entities/Category.cs`)**
   - Add new properties or domain methods to the `Category` class.

2. **Configure Database Schema (`WFAI.Infrastructure/Persistence/DbConfigurations/CategoryConfiguration.cs`)**
   - Configure field constraints, max lengths, defaults, relationships, or indexes.
   - Run EF Core CLI to create a migration:
     `dotnet ef migrations add Add<FeatureName>ToCategory --project WFAI.Infrastructure --startup-project WFAI.API`

3. **Create CQRS Command/Query & Handler (`WFAI.Application/Features/Categories/`)**
   - Define command record implementing `IRequest<IResponseWrapper<TResult>>` and `IValidateMe`.
   - Implement handler using `IRequestHandler<TCommand, IResponseWrapper<TResult>>`.
   - Write key normalization, uniqueness guards, outbox message queuing, and cache invalidation.

4. **Add Validation (`WFAI.Application/Features/Categories/Commands/...`)**
   - Inherit from `AbstractValidator<TCommand>` using FluentValidation. Define clear error messages.

5. **Expose Endpoint (`WFAI.API/Endpoints/CategoryEndpoints.cs`)**
   - Register route on the category Minimal API route group.
   - Specify `.Produces<IResponseWrapper<T>>()` and `.RequireAuthorization(...)`.

6. **Add Frontend API Request (`WFAI.Client/src/lib/categories-api.ts`)**
   - Add Request/Response interfaces and export endpoint method on `categories-api`.

7. **Create React Query Custom Hook (`WFAI.Client/src/hooks/useCategories.ts`)**
   - Wrap API method in `useQuery` or `useMutation`.
   - Configure toast notifications and query invalidations (`queryClient.invalidateQueries({ queryKey: ['categories'] })`).

8. **Update UI Page / Component (`WFAI.Client/src/pages/CategoriesManagement.tsx`)**
   - Connect UI elements, modal forms, and tables to the new custom hook.

9. **Add Tests (`WFAI.Application.Tests` & `WFAI.Domain.Tests`)**
   - Create handler and validation test cases using `CategoryHandlerTestScope`.

---

## Code Patterns

### 1. Category Domain Entity (`Category.cs`)
```csharp
namespace WFAI.Domain.Entities
{
    public class Category : BaseEntity<int>, IFullEntity, IDataConcurrency
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;
        public string NormalizedSlug { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> Children { get; set; } = new HashSet<Category>();
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public bool SoftDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? LastModifiedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
```

### 2. CQRS Command & Handler (`CreateCategoryCommand.cs`)
```csharp
namespace WFAI.Application.Features.Categories.Commands.Create
{
    public record CreateCategoryCommand(
        string Name,
        string Slug,
        int? ParentId,
        bool IsActive,
        int SortOrder
    ) : IRequest<IResponseWrapper<int>>, IValidateMe;

    public class CreateCategoryCommandHandler(
        IApplicationDbContext applicationDbContext,
        ICacheService cacheService)
       : IRequestHandler<CreateCategoryCommand, IResponseWrapper<int>>
    {
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;
        private readonly ICacheService _cacheService = cacheService;

        public async ValueTask<IResponseWrapper<int>> Handle(CreateCategoryCommand request, CancellationToken ct)
        {
            var normalizedName = CategoryWriteGuards.NormalizeKey(request.Name);
            var normalizedSlug = CategoryWriteGuards.NormalizeKey(request.Slug);

            if (await _applicationDbContext.Categories.AnyAsync(o => o.NormalizedName == normalizedName, ct))
            {
                return ResponseWrapper<int>.Fail("Category with this name already exists.");
            }

            var category = new Category
            {
                Name = request.Name.Trim(),
                NormalizedName = normalizedName,
                Slug = request.Slug.Trim(),
                NormalizedSlug = normalizedSlug,
                ParentId = request.ParentId,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder,
                RowVersion = [0]
            };

            await _applicationDbContext.Categories.AddAsync(category, ct);
            await _applicationDbContext.SaveChangesAsync(ct);

            _applicationDbContext.AddOutboxMessage(new CategoryCreatedEvent(category.Id));
            await _applicationDbContext.SaveChangesAsync(ct);

            foreach (var key in CategoryCacheKeys.All)
            {
                _cacheService.Remove(key);
            }

            return ResponseWrapper<int>.Success(category.Id, "Category created successfully.");
        }
    }
}
```

### 3. Command Validation (`CreateCategoryCommandValidator.cs`)
```csharp
namespace WFAI.Application.Features.Categories.Commands.Create
{
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug is required.")
                .MaximumLength(150).WithMessage("Slug cannot exceed 150 characters.");

            RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("SortOrder must be greater than or equal to 0.");
        }
    }
}
```

### 4. Minimal API Endpoint (`CategoryEndpoints.cs`)
```csharp
group.MapPost("/", async (ISender sender, CreateCategoryRequest request, CancellationToken ct) =>
{
    var command = new CreateCategoryCommand(
        request.Name,
        request.Slug,
        request.ParentId,
        request.IsActive,
        request.SortOrder);
    var response = await sender.Send(command, ct);
    return response.ToApiResult();
})
.Produces<IResponseWrapper>()
.WithName("CreateCategory")
.RequireAuthorization(AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.Create));
```

### 5. Frontend API Function (`categories-api.ts`)
```typescript
export const categoriesApi = {
  create: (data: CreateCategoryRequest): Promise<ApiResponse<number>> => {
    return api.post('api/v1/categories', data);
  },
};
```

### 6. React Query Custom Hook (`useCategories.ts`)
```typescript
export function useCreateCategory() {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (data: CreateCategoryRequest) => categoriesApi.create(data),
    onSuccess: (response) => {
      if (response.isSuccessful) {
        toast.success('Category created successfully!');
        queryClient.invalidateQueries({ queryKey: ['categories'] });
      } else {
        toast.error(response.messages[0] || 'Failed to create category.');
      }
    },
    onError: (err: Error) => {
      toast.error(err.message || 'An error occurred during save.');
    },
  });
}
```

### 7. Unit Test (`CategoryCommandHandlerTests.cs`)
```csharp
[Fact]
public async Task Handle_should_create_category_add_outbox_message_and_clear_category_caches()
{
    await using var scope = await CategoryHandlerTestScope.CreateAsync();
    var handler = new CreateCategoryCommandHandler(scope.DbContext, scope.Cache);
    var command = new CreateCategoryCommand("Electronics", "electronics", null, true, 1);

    var result = await handler.Handle(command, CancellationToken.None);

    result.IsSuccessful.Should().BeTrue();
    var category = await scope.DbContext.Categories.SingleAsync();
    category.Name.Should().Be("Electronics");
    scope.Cache.RemovedKeys.Should().BeEquivalentTo(CategoryCacheKeys.All);
}
```

---

## Testing Checklist

- [ ] Category entity has proper property types, constraints, and audit properties.
- [ ] Database configuration handles indexes, max lengths, and soft delete filters (`[SoftDeleted] = 0`).
- [ ] FluentValidation correctly validates missing or invalid payload inputs.
- [ ] Command handlers normalize keys (`NormalizedName`, `NormalizedSlug`) before checking duplicates.
- [ ] Outbox messages (`AddOutboxMessage`) are queued upon successful write operations.
- [ ] Cache keys (`CategoryCacheKeys.All`) are cleared after any mutation command.
- [ ] Minimal API routes correctly declare `.Produces<...>()` and `.RequireAuthorization(...)`.
- [ ] Frontend API wrapper (`categories-api.ts`) targets correct endpoints and types.
- [ ] React Query custom hooks trigger toast notifications and call `invalidateQueries({ queryKey: ['categories'] })`.
- [ ] Automated tests in `WFAI.Application.Tests` pass with high coverage.

---

## Common Mistakes

1. **Forgetting Key Normalization**: Performing duplicate checks on raw user input instead of normalized keys (`NormalizedName`, `NormalizedSlug`), causing casing mismatches.
2. **Missing Soft Delete Filter on Indexes**: Creating standard unique indexes without `.HasFilter("[SoftDeleted] = 0")`, which prevents creating a category with the name of a soft-deleted item.
3. **Skipping Outbox Messages**: Forgetting `_applicationDbContext.AddOutboxMessage(...)`, breaking downstream event handlers and sync processes.
4. **Omitting Cache Invalidation**: Not clearing `CategoryCacheKeys.All` in mutation handlers, leading to stale query results.
5. **Ignoring Concurrency Tokens**: Failing to update or validate `RowVersion` during edit/update commands, allowing silent overwrites.
6. **Hardcoding Authorization Strings**: Hand-writing permission strings instead of using `AppPermission.NameFor(AppService.Product, AppFeature.Categories, AppAction.<Action>)`.
7. **Frontend Cache Stale State**: Forgetting `queryClient.invalidateQueries({ queryKey: ['categories'] })` after React Query mutations.
