namespace WFAI.Application.Features.Users.Models.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsLocked { get; set; }
    }
}