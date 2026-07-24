namespace WFAI.Domain.Interfaces
{
    /// <summary>
    /// Contract for entities that support soft deletion.
    /// </summary>
    public interface ISoftDelete
    {
        /// <summary>
        /// Indicates whether the entity has been soft deleted.
        /// </summary>
        public bool SoftDeleted { get; set; }

        /// <summary>
        /// Identifier of the user who performed the delete operation.
        /// </summary>
        public int? DeletedBy { get; set; }

        /// <summary>
        /// Timestamp when the entity was soft deleted.
        /// </summary>
        DateTime? DeletedAt { get; set; }
    }
}