namespace WFAI.Domain.Entities
{
    public class Phase : BaseEntity<int>, IFullEntity, IDataConcurrency
    {
        public string Title { get; set; } = string.Empty;

        public string NormalizedTitle { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public bool SoftDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedAt { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? LastModifiedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
