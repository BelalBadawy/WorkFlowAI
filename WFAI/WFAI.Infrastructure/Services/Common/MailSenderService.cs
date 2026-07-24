using FluentEmail.Core;
using FluentEmail.Smtp;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using WFAI.Application.Dtos.Common;
using WFAI.Application.Dtos.Email;
using WFAI.Application.Interfaces.Common;

namespace WFAI.Infrastructure.Services.Common
{
    public class MailSenderService : IEmailService
    {
        private readonly EmailConfiguration _emailSettings;

        public MailSenderService(IOptions<EmailConfiguration> emailConfig)
        {
            _emailSettings = emailConfig.Value;
        }

        public async Task<string> SendAsync(SendEmailDto request, CancellationToken ct)
        {
            var attachmentStreams = new List<MemoryStream>();

            try
            {
                var smtpClient = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
                {
                    Credentials = new NetworkCredential(_emailSettings.Email, _emailSettings.Password),
                    EnableSsl = _emailSettings.EnableSsl
                };
                Email.DefaultSender = new SmtpSender(smtpClient);

                var email = Email
                    .From(_emailSettings.Email, _emailSettings.DisplayName)
                    .Subject(request.Subject)
                    .Body(request.MessageBody, isHtml: true);

                if (request.ToEmails?.Any() == true)
                {
                    foreach (var to in request.ToEmails)
                    {
                        email.To(to);
                    }
                }
                else if (!string.IsNullOrEmpty(request.MailTo))
                {
                    email.To(request.MailTo);
                }

                if (request.EmailCC?.Any() == true)
                {
                    foreach (var cc in request.EmailCC)
                    {
                        email.CC(cc);
                    }
                }

                if (request.EmailBCC?.Any() == true)
                {
                    foreach (var bcc in request.EmailBCC)
                    {
                        email.BCC(bcc);
                    }
                }

                if (request.Attachments?.Any() == true)
                {
                    var attachmentList = new List<FluentEmail.Core.Models.Attachment>();

                    foreach (FileData file in request.Attachments)
                    {
                        if (file.Length <= 0)
                        {
                            continue;
                        }

                        var stream = new MemoryStream();
                        attachmentStreams.Add(stream);

                        if (file.Content.CanSeek)
                        {
                            file.Content.Position = 0;
                        }

                        await file.Content.CopyToAsync(stream, ct);
                        stream.Position = 0;

                        attachmentList.Add(new FluentEmail.Core.Models.Attachment
                        {
                            Data = stream,
                            Filename = file.FileName,
                            ContentType = file.ContentType
                        });
                    }

                    if (attachmentList.Any())
                    {
                        email.Attach(attachmentList);
                    }
                }

                var response = await email.SendAsync(ct);

                return response.Successful
                    ? string.Empty
                    : string.Join("; ", response.ErrorMessages);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                foreach (var stream in attachmentStreams)
                {
                    await stream.DisposeAsync();
                }
            }
        }
    }
}