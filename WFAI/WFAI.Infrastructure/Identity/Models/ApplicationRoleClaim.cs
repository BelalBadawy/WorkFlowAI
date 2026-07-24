namespace WFAI.Infrastructure.Identity.Models
{
    public class ApplicationRoleClaim : IdentityRoleClaim<int>
    {
        [MaxLength(256)]
        public string Description { get; set; } = string.Empty;

    }
}