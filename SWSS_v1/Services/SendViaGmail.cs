using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
namespace SWSS_v1.Services
{
    public class SendViaGmail : IMailCommunication
    {
        //
        public async Task Send(string from, string to, string subject, string body, string appPasswprd)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            // Use Port 587 with StartTls for the most secure connection
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

            // Authenticate using your email and the generated App Password
            await smtp.AuthenticateAsync(from, appPasswprd);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
