using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WFAI.Application.Interfaces.Common;
using WFAI.Domain.Entities;
using WFAI.Domain.Interfaces;

namespace WFAI.Application.Tests.Support.Categories;

internal sealed class RecordingCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _values = new();

    public List<string> RemovedKeys { get; } = [];
    public List<string> SetKeys { get; } = [];

    public bool TryGet<T>(string cacheKey, out T value)
    {
        if (_values.TryGetValue(cacheKey, out var cached) && cached is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }

    public T Set<T>(string cacheKey, T value)
    {
        _values[cacheKey] = value;
        SetKeys.Add(cacheKey);
        return value;
    }

    public void Remove(string cacheKey)
    {
        _values.Remove(cacheKey);
        RemovedKeys.Add(cacheKey);
    }
}

internal sealed class CategoryHandlerTestDbContext : DbContext, IApplicationDbContext
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public CategoryHandlerTestDbContext(DbContextOptions<CategoryHandlerTestDbContext> options)
        : base(options)
    {
    }

    public bool ThrowConcurrencyOnSave { get; set; }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<LogUserActivity> LogUserActivities => Set<LogUserActivity>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<User> Users => Set<User>();

    public Task StartTransaction(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitTransaction(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackTransaction(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowConcurrencyOnSave)
        {
            ThrowConcurrencyOnSave = false;
            throw new DbUpdateConcurrencyException();
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public void AddOutboxMessage<TNotification>(TNotification notification) where TNotification : class
    {
        var notificationType = notification.GetType();

        OutboxMessages.Add(new OutboxMessage
        {
            Type = notificationType.AssemblyQualifiedName ?? notificationType.FullName ?? notificationType.Name,
            Payload = JsonSerializer.Serialize(notification, notificationType, SerializerOptions),
            OccurredOnUtc = DateTime.UtcNow
        });
    }

    public void SetOriginalRowVersion<TEntity>(TEntity entity, byte[] rowVersion) where TEntity : class, IDataConcurrency
    {
        Entry(entity).Property(x => x.RowVersion).OriginalValue = rowVersion;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("Categories");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
            builder.Property(x => x.NormalizedName).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Slug).IsRequired().HasMaxLength(250);
            builder.Property(x => x.NormalizedSlug).IsRequired().HasMaxLength(256);
            builder.Property(x => x.RowVersion).IsConcurrencyToken();
            builder.HasIndex(x => x.NormalizedName).HasFilter("SoftDeleted = 0").IsUnique().HasDatabaseName("UX_Categories_NormalizedName");
            builder.HasIndex(x => x.NormalizedSlug).HasFilter("SoftDeleted = 0").IsUnique().HasDatabaseName("UX_Categories_NormalizedSlug");
            builder.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasQueryFilter(x => !x.SoftDeleted);
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        });
    }
}

internal sealed class CategoryHandlerTestScope : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private CategoryHandlerTestScope(SqliteConnection connection, CategoryHandlerTestDbContext dbContext)
    {
        _connection = connection;
        DbContext = dbContext;
        Cache = new RecordingCacheService();
    }

    public CategoryHandlerTestDbContext DbContext { get; }
    public RecordingCacheService Cache { get; }

    public static async Task<CategoryHandlerTestScope> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CategoryHandlerTestDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var dbContext = new CategoryHandlerTestDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return new CategoryHandlerTestScope(connection, dbContext);
    }

    public async Task<Category> SeedCategoryAsync(
        string name,
        string slug,
        int sortOrder,
        bool isActive = true,
        int? parentId = null,
        bool softDeleted = false,
        byte[]? rowVersion = null)
    {
        var category = new Category
        {
            Name = name,
            Slug = slug,
            NormalizedName = name.Trim().ToUpperInvariant(),
            NormalizedSlug = slug.Trim().ToUpperInvariant(),
            SortOrder = sortOrder,
            IsActive = isActive,
            ParentId = parentId,
            SoftDeleted = softDeleted,
            RowVersion = rowVersion ?? [1]
        };

        DbContext.Categories.Add(category);
        await DbContext.SaveChangesAsync();
        return category;
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}