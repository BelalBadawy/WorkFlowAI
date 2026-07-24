namespace WFAI.Domain.Entities
{
    public class Category : BaseEntity<int>, IFullEntity, IDataConcurrency
    {
        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string NormalizedName { get; set; } = string.Empty;

        public string NormalizedSlug { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        public virtual Category? Parent { get; set; }

        public virtual ICollection<Category> Children { get; set; } = new HashSet<Category>();

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