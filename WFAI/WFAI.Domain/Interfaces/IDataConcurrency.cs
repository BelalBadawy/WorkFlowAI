namespace WFAI.Domain.Interfaces
{
    /// <summary>
    /// Contract for entities that support optimistic concurrency via a row version.
    /// Implementations should have a timestamp/row-version column in the database
    /// which is used to detect conflicting updates.
    /// Note: The [Timestamp] attribute should be applied in the Infrastructure layer
    /// via IEntityTypeConfiguration, not here in the domain.
    /// </summary>
    public interface IDataConcurrency
    {
        /// <summary>
        /// Row version used for optimistic concurrency control. Typically mapped to a
        /// database timestamp/rowversion column.
        /// </summary>
        byte[] RowVersion { get; set; }
    }
}