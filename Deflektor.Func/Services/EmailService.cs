using Azure.Messaging.ServiceBus;
using Deflektor.Func.Interfaces;
using Deflektor.Func.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Deflektor.Func.Services;

/// <summary>
/// Service for processing emails
/// </summary>
public class EmailService : IEmailService
{
    private readonly IGraphService _graphService;
    private readonly IDeflektorEngineService _deflektorEngineService;
    private readonly ILogger<EmailService> _logger;
    private readonly AppSettings _settings;

    /// <summary>
    /// Initializes a new instance of the EmailService
    /// </summary>
    /// <param name="graphService">Graph service instance</param>
    /// <param name="deflektorEngineService">Deflektor engine service instance</param>
    /// <param name="options">Application settings</param>
    /// <param name="logger">Logger instance</param>
    public EmailService(
        IGraphService graphService,
        IDeflektorEngineService deflektorEngineService,
        IOptions<AppSettings> options,
        ILogger<EmailService> logger)
    {
        _graphService = graphService;
        _deflektorEngineService = deflektorEngineService;
        _settings = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ProcessWebhookNotification(string requestBody)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<WebhookPayload>(requestBody);

            if (payload?.Value == null || payload.Value.Count == 0)
            {
                _logger.LogWarning("No valid payload received");
                return false;
            }

            var resourcePath = payload.Value.First().Resource;

            var mailId = payload.Value.First().ResourceData?.Id;
            var emailMessage = await GetEmailDetails(mailId);
            
            if (emailMessage != null)
            {
                return await SendToProcessingQueue(emailMessage);
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook notification");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<EmailMessage> GetEmailDetails(string messageId)
    {
        try
        {
            var email = await _graphService.GetEmailById(messageId);

            if (email == null)
            {
                _logger.LogWarning("Email not found for resource: {messageId}", messageId);
                return null;
            }

            // Process the email body (removing HTML tags)
            var body = email.Body.Content;
            body = Regex.Replace(body, "<.*?>", string.Empty);
            body = Regex.Replace(body, "<!--.*?-->", string.Empty);

            var subject = email.Subject;
            var senderEmail = email.Sender.EmailAddress.Address;
            var recipient = email.ToRecipients.FirstOrDefault()?.EmailAddress.Address ?? _settings.Email.SupportEmailAddress;

            _logger.LogInformation("Email received: {Subject} from {Sender}", subject, senderEmail);

            return new EmailMessage
            {
                EmailId = email.Id,
                Subject = subject,
                Body = body,
                Sender = senderEmail,
                Recipient = recipient
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email details from resource path: {messageId}", messageId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendToProcessingQueue(EmailMessage emailMessage)
    {
        var connectionString = _settings.ServiceBus.ConnectionString;
        var queueName = _settings.ServiceBus.QueueName;
        
        try
        {
            await using var client = new ServiceBusClient(connectionString);
            await using var messageSender = client.CreateSender(queueName);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var serviceBusMessageBody = JsonSerializer.Serialize(emailMessage, options);

            var sbMessage = new ServiceBusMessage(serviceBusMessageBody);

            _logger.LogInformation("Sending email to Service Bus queue: {QueueName}", queueName);
            await messageSender.SendMessageAsync(sbMessage);
            _logger.LogInformation("Email successfully sent to Service Bus queue");
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Service Bus queue");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ProcessQueuedEmail(EmailMessage emailMessage)
    {
        try
        {
            _logger.LogInformation("Processing email: {Subject} from {Sender}", emailMessage.Subject, emailMessage.Sender);
            
            // Process the email using DeflektorEngineService
            var response = await _deflektorEngineService.ElaborateTicket(emailMessage.Body);
            
            // Get Graph client
            var reply = CreateReplyMessage(emailMessage.Subject, response.Text, emailMessage.Sender);

            // Get recipient user ID
            if (string.IsNullOrEmpty(emailMessage.Recipient))
            {
                _logger.LogWarning("No recipient specified in the email message");
                return false;
            }

            var recipientUserId = await _graphService.GetUserIdFromEmail(emailMessage.Recipient);

            if (string.IsNullOrEmpty(recipientUserId))
            {
                _logger.LogWarning("Could not find user with email {Recipient}", emailMessage.Recipient);
                return false;
            }

            // Send the reply
            await _graphService.ReplyToEmail(recipientUserId, emailMessage.EmailId, reply);
            _logger.LogInformation("Reply sent successfully to {Sender}", emailMessage.Sender);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email from queue");
            return false;
        }
    }

    private Message CreateReplyMessage(string subject, string content, string recipientEmail)
    {
        return new Message
        {
            Subject = $"{_settings.Email.ReplySubjectPrefix}{subject}",
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = content
            },
            ToRecipients = new List<Recipient>
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = recipientEmail
                    }
                }
            }
        };
    }
}