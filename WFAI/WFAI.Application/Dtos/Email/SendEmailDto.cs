using WFAI.Application.Dtos.Common;

namespace WFAI.Application.Dtos.Email
{
    public class SendEmailDto
    {
        public string MailTo { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string MessageBody { get; set; } = string.Empty;

        public IList<FileData> Attachments { get; set; } = new List<FileData>();

        public IEnumerable<string> ToEmails { get; set; } = new List<string>();

        public IEnumerable<string> EmailCC { get; set; } = new List<string>();

        public IEnumerable<string> EmailBCC { get; set; } = new List<string>();

        public string Priority { get; set; } = string.Empty;
    }
}