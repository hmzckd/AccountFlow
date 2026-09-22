using AccountFlow.Backend.Models;

namespace AccountFlow.Backend.Services
{
    // Defines the contract for email transmission operations
    public interface IEmailService
    {
        // Asynchronously sends an email using the provided email data
        Task SendMailAsync(EmailDto request);
    }
}
