namespace WFAI.Domain.Entities
{
    public class AuditTrail : BaseEntity<int>
    {
        public int? UserId { get; set; }
        public string? IpAddress { get; set; }
        public AuditType Type { get; set; }
        public string? TableName { get; set; }
        public DateTime DateTime { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? AffectedColumns { get; set; }
        public string? PrimaryKey { get; set; }
    }
}