namespace WFAI.Domain.Interfaces
{
    /// <summary>
    /// Base contract for entities that are multi-tenant, auditable, 
    /// support soft delete and optimistic concurrency.
    /// </summary>
    public interface IFullEntity : IAuditable, ISoftDelete, IDataConcurrency
    {

    }
}