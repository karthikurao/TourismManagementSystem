using Microsoft.AspNetCore.Identity.UI.Services;

namespace TourismManagementSystem.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // TODO: Implement actual email sending logic here
            // For now, just log the email
            Console.WriteLine($"Email would be sent to: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {htmlMessage}");
            
            return Task.CompletedTask;
        }
    }
}