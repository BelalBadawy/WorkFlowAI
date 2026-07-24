using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using WFAI.Domain.Enums;
using static WFAI.Application.Enums.AppEnums;

namespace WFAI.Infrastructure.Persistence.Audit
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }
        public int? UserId { get; set; }
        public string? IpAddress { get; set; }
        public string? TableName { get; set; }

        // âœ… Changed from string? to AuditType
        public AuditType Type { get; set; }

        public Dictionary<string, object> KeyValues { get; } = new();
        public Dictionary<string, object> OldValues { get; } = new();
        public Dictionary<string, object> NewValues { get; } = new();
        public List<PropertyEntry> TemporaryProperties { get; } = new();

        public bool HasTemporaryProperties => TemporaryProperties.Any();

        public AuditTrail ToAudit()
        {
            var audit = new AuditTrail
            {
                UserId = UserId,
                IpAddress = IpAddress,
                Type = Type,
                TableName = TableName,
                DateTime = DateTime.UtcNow,
                PrimaryKey = JsonSerializer.Serialize(KeyValues),
                OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
                NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
                AffectedColumns = TemporaryProperties.Count == 0 ? null : JsonSerializer.Serialize(TemporaryProperties.Select(p => p.Metadata.Name).ToList())
            };
            return audit;
        }
    }
}