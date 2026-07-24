namespace WFAI.Application.Features.Users.Commands
{
    public class UpdateUserRequest
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
    }
}