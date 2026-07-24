namespace WFAI.Infrastructure.Identity.Configurations
{
    public class SeedUsersConfiguration
    {
        public SeedUserConfiguration Admin { get; set; } = new();
        public SeedUserConfiguration Basic { get; set; } = new();
    }

    public class SeedUserConfiguration
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}