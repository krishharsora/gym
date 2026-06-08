using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using cafe.Models;



public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public void SendEmail(string toEmail, string subject, string body)
    {
        var smtp = new SmtpClient(_settings.Host)
        {
            Port = _settings.Port,
            Credentials = new NetworkCredential(_settings.Mail, _settings.Password),
            EnableSsl = true
        };

        var mail = new MailMessage
        {
            From = new MailAddress(_settings.Mail),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        smtp.Send(mail);
    }
  
}