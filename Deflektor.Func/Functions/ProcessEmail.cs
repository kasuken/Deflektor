using Azure.Messaging.ServiceBus;
using Deflektor.Func.Interfaces;
using Deflektor.Func.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Deflektor.Func.Functions;

/// <summary>
/// Function to process emails from the Service Bus queue
/// </summary>
public class ProcessEmail
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessEmail> _logger;

    /// <summary>
    /// Initializes a new instance of the ProcessEmail function
    /// </summary>
    /// <param name="emailService">Email service instance</param>
    /// <param name="logger">Logger instance</param>
    public ProcessEmail(
        IEmailService emailService,
        ILogger<ProcessEmail> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Processes email messages from the Service Bus queue
    /// </summary>
    /// <param name="message">The received Service Bus message</param>
    /// <param name="messageActions">Service Bus message actions</param>
    [Function(nameof(ProcessEmail))]
    public async Task Run(
        [ServiceBusTrigger("deflektor-queue-dev001", Connection = "ServiceBus:ConnectionString")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Processing Service Bus message - ID: {id}", message.MessageId);
        
        try
        {
            // Deserialize the message body to EmailMessage object
            var messageJson = message.Body.ToString();
            if (string.IsNullOrEmpty(messageJson))
            {
                _logger.LogWarning("Received empty message body");
                await messageActions.DeadLetterMessageAsync(message);
                return;
            }

            var emailMessage = JsonSerializer.Deserialize<EmailMessage>(messageJson);

            if (emailMessage == null)
            {
                _logger.LogWarning("Could not deserialize email message from queue");
                await messageActions.DeadLetterMessageAsync(message);
                return;
            }

            var success = await _emailService.ProcessQueuedEmail(emailMessage);
            
            if (success)
            {
                _logger.LogInformation("Successfully processed email {EmailId}", emailMessage.EmailId);
                await messageActions.CompleteMessageAsync(message);
            }
            else
            {
                _logger.LogWarning("Failed to process email {EmailId}", emailMessage.EmailId);
                await messageActions.DeadLetterMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email from queue");
            await messageActions.DeadLetterMessageAsync(message);
        }
    }
}