using WFAI.Domain.Common;

namespace WFAI.Domain.Entities
{
    public class User : BaseEntity<int>
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}