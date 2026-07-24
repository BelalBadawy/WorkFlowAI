namespace WFAI.Domain.Interfaces
{
    /// <summary>
    /// Contract for entities that maintain audit information such as who created
    /// or modified the entity and when those actions occurred.
    /// </summary>
    public interface IAuditable
    {
        /// <summary>
        /// Identifier for the user who created the entity, if available.
        /// </summary>
        public int? CreatedBy { get; set; }

        /// <summary>
        /// Creation timestamp for the entity.
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Identifier for the user who last modified the entity, if available.
        /// </summary>
        public int? LastModifiedBy { get; set; }

        /// <summary>
        /// Last modification timestamp for the entity.
        /// </summary>
        DateTime? LastModifiedAt { get; set; }
    }
}