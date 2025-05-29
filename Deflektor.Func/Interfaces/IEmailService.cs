using Deflektor.Func.Models;

namespace Deflektor.Func.Interfaces;

/// <summary>
/// Interface for email processing operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Processes an incoming email webhook notification
    /// </summary>
    /// <param name="requestBody">The request body from the webhook</param>
    /// <returns>True if successfully processed and sent to queue</returns>
    Task<bool> ProcessWebhookNotification(string requestBody);

    /// <summary>
    /// Gets email details from Microsoft Graph using the resource path
    /// </summary>
    /// <param name="resourcePath">The Graph API resource path for the email</param>
    /// <returns>Processed email message details</returns>
    Task<EmailMessage> GetEmailDetails(string resourcePath);

    /// <summary>
    /// Sends an email to the Service Bus queue for processing
    /// </summary>
    /// <param name="emailMessage">The email message to enqueue</param>
    /// <returns>True if successfully sent to queue</returns>
    Task<bool> SendToProcessingQueue(EmailMessage emailMessage);

    /// <summary>
    /// Processes an email from the queue and sends a reply
    /// </summary>
    /// <param name="emailMessage">The email message to process</param>
    /// <returns>True if successfully processed and replied</returns>
    Task<bool> ProcessQueuedEmail(EmailMessage emailMessage);
}