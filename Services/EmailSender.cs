using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace YardOps.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            
            var host = smtpSettings["Host"];
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"] ?? "YardOps";
            var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail!, fromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                
                mailMessage.To.Add(email);

                // Fix email threading: Add unique Message-ID header
                mailMessage.Headers.Add("Message-ID", $"<{Guid.NewGuid()}@yardops.com>");
                
                // Prevent threading by adding unique headers
                mailMessage.Headers.Add("X-Entity-Ref-ID", Guid.NewGuid().ToString());
                
                // Add timestamp to make each email unique
                mailMessage.Headers.Add("Date", DateTime.UtcNow.ToString("R"));

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", email, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                throw;
            }
        }
    }
}