using System.Reflection;

namespace WFAI.Domain.Tests.Support;

internal static class EntityTestExtensions
{
    public static TEntity WithId<TEntity, TId>(this TEntity entity, TId id)
    {
        var type = typeof(TEntity);
        PropertyInfo? propertyInfo = null;

        while (propertyInfo is null && type is not null)
        {
            propertyInfo = type.GetProperty(
                "Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            type = type.BaseType;
        }

        propertyInfo.Should().NotBeNull();

        if (propertyInfo!.CanWrite)
        {
            propertyInfo.SetValue(entity, id);
            return entity;
        }

        var backingField = propertyInfo.DeclaringType?.GetField(
            "<Id>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        backingField.Should().NotBeNull();
        backingField!.SetValue(entity, id);
        return entity;
    }
}