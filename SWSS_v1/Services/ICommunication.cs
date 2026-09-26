using MimeKit;

namespace SWSS_v1.Services
{
    //via gmail, zohomail etc
    public interface IMailCommunication 
    {
        public Task Send(string from, string to, string subject, string body, string appPassword);
        Task SendAttachement(string from, string to, string subject, MimeMessage message, string appPasswprd);
    }
}
