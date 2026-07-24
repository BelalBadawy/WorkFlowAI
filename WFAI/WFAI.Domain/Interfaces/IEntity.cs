namespace WFAI.Domain.Interfaces
{
    /// <summary>
    /// Generic entity contract exposing a typed identifier.
    /// </summary>
    /// <typeparam name="TId">Type of the entity identifier.</typeparam>
    public interface IEntity<TId> : IEntity
    {
        TId Id { get; }
    }

    /// <summary>
    /// Marker interface for entities.
    /// </summary>
    public interface IEntity
    {

    }
}