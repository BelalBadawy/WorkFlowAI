namespace WFAI.Infrastructure.Identity.Models
{
    public class ApplicationRole : IdentityRole<int>
    {
        [MaxLength(256)]
        public string Description { get; set; } = string.Empty;
    }
}