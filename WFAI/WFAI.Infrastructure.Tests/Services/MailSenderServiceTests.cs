using FluentEmail.Core;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Reflection;
using WFAI.Application.Dtos.Email;
using WFAI.Infrastructure.Services.Common;

namespace WFAI.Infrastructure.Tests.Services;

public class MailSenderServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_should_configure_smtp_ssl_from_options(bool enableSsl)
    {
        var options = Options.Create(new EmailConfiguration
        {
            Host = "127.0.0.1",
            Port = 1,
            Email = "sender@example.com",
            Password = "password",
            DisplayName = "Sender",
            EnableSsl = enableSsl
        });
        var service = new MailSenderService(options);

        await service.SendAsync(
            new SendEmailDto
            {
                MailTo = "receiver@example.com",
                Subject = "Test",
                MessageBody = "Body"
            },
            CancellationToken.None);

        var sender = Email.DefaultSender;
        sender.Should().NotBeNull();
        var smtpClientField = sender.GetType()
            .GetField("_smtpClient", BindingFlags.Instance | BindingFlags.NonPublic);
        smtpClientField.Should().NotBeNull();

        var smtpClientValue = smtpClientField!.GetValue(sender);
        smtpClientValue.Should().BeOfType<SmtpClient>();
        var smtpClient = (SmtpClient)smtpClientValue!;

        smtpClient.EnableSsl.Should().Be(enableSsl);
    }
}