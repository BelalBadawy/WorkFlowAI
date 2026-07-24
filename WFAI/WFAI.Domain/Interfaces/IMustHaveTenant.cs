namespace WFAI.Domain.Interfaces
{
    /// <summary>
    /// Contract for entities that belong to a tenant in a multi-tenant application.
    /// </summary>
    public interface IMustHaveTenant
    {
        /// <summary>
        /// Tenant identifier that owns the entity.
        /// </summary>
        public int TenantId { get; set; }
    }
}