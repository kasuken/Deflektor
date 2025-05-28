using Azure.Messaging.ServiceBus;
using Deflektor.Func.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Deflektor.Func;

public class ProcessEmail
{
    private readonly GraphService _graphService;
    private readonly DeflektorEngineService _deflektorEngineService;
    private readonly ILogger<ProcessEmail> _logger;

    public ProcessEmail(
        ILogger<ProcessEmail> logger,
        GraphService graphService,
        DeflektorEngineService deflektorEngineService)
    {
        _logger = logger;
        _graphService = graphService;
        _deflektorEngineService = deflektorEngineService;
    }

    [Function(nameof(ProcessEmail))]
    public async Task Run(
        [ServiceBusTrigger("deflektor-queue-dev001", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        // Deserialize the message body to EmailMessage object
        var messageJson = message.Body.ToString();
        if (string.IsNullOrEmpty(messageJson))
        {
            _logger.LogWarning("Received empty message body");
            return;
        }

        var emailMessage = JsonSerializer.Deserialize<EmailMessage>(messageJson);

        if (emailMessage == null)
        {
            _logger.LogWarning("Could not deserialize email message from queue");
            return;
        }

        _logger.LogInformation("Processing email: {Subject} from {Sender}", emailMessage.Subject, emailMessage.Sender);

        // Process the email using DeflektorEngineService
        var response = await _deflektorEngineService.ElaborateTicket(emailMessage.Body);

        // Get Graph client
        var graphClient = _graphService.GetGraphServiceClient();

        // Prepare the reply
        var reply = new Message
        {
            Subject = "Re from Deflektor: " + emailMessage.Subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Text,
                Content = response.Text
            },
            ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = emailMessage.Sender
                            }
                        }
                    }
        };

        // Get recipient user ID
        if (!string.IsNullOrEmpty(emailMessage.Recipient))
        {
            var recipientUserId = (await graphClient.Users.Request()
                .Filter($"mail eq '{emailMessage.Recipient}'")
                .Select("id")
                .GetAsync()).FirstOrDefault()?.Id;

            if (!string.IsNullOrEmpty(recipientUserId))
            {
                // Send the reply
                await graphClient.Users[recipientUserId].Messages[emailMessage.EmailId]
                    .Reply(reply)
                    .Request()
                    .PostAsync();

                _logger.LogInformation("Reply sent successfully to {Sender}", emailMessage.Sender);
            }
            else
            {
                _logger.LogWarning("Could not find user with email {Recipient}", emailMessage.Recipient);
            }
        }
        else
        {
            _logger.LogWarning("No recipient specified in the email message");
        }

        // Complete the message
        await messageActions.CompleteMessageAsync(message);
    }
}