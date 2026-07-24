using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

namespace WFAI.Infrastructure.Extensions
{
    public static class QueryExtensions
    {
        public static void AddSoftDeleteQueryFilter(this IMutableEntityType entityType)
        {
            // Skip if entity does not implement ISoftDelete
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                return;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var prop = Expression.Property(parameter, nameof(ISoftDelete.SoftDeleted));
            var filter = Expression.Lambda(Expression.Equal(prop, Expression.Constant(false)), parameter);

            entityType.SetQueryFilter(filter);

            // Add index if not already defined
            var property = entityType.FindProperty(nameof(ISoftDelete.SoftDeleted));
            if (property != null && entityType.GetIndexes().All(i => !i.Properties.Contains(property)))
            {
                entityType.AddIndex(property);
            }
        }

        //public static void AddTenantQueryFilter(this IMutableEntityType entityType, string tenantId)
        //{
        //    // Skip if entity does not implement ITenant
        //    if (!typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
        //        return;

        //    var parameter = Expression.Parameter(entityType.ClrType, "e");
        //    var prop = Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));
        //    var filter = Expression.Lambda(Expression.Equal(prop, Expression.Constant(tenantId)), parameter);
        //    entityType.SetQueryFilter(filter);

        //    // Add index if not already defined
        //    var property = entityType.FindProperty(nameof(IMustHaveTenant.TenantId));
        //    if (property != null && entityType.GetIndexes().All(i => !i.Properties.Contains(property)))
        //    {
        //        entityType.AddIndex(property);
        //    }
        //}

    }

}