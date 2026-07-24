namespace WFAI.Domain.Common
{
    /// <summary>
    /// Base entity class that provides a generic Id property and domain event handling.
    /// </summary>
    /// <typeparam name="TId">Type of the entity identifier.</typeparam>
    public abstract class BaseEntity<TId> : IEntity<TId> where TId : notnull
    {
        /// <summary>
        /// Primary identifier for the entity.
        /// </summary>
        public TId Id { get; protected set; } = default!;
    }
}