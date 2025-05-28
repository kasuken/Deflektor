using Azure.Messaging.ServiceBus;
using Deflektor.Func.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deflektor.Func;

public class ElaborateEmail
{
    private readonly GraphService _graphService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ElaborateEmail> _logger;

    public ElaborateEmail(ILogger<ElaborateEmail> logger, GraphService graphService, IConfiguration configuration)
    {
        _logger = logger;
        _graphService = graphService;
        _configuration = configuration;
    }

    [Function("ElaborateEmail")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        var validationToken = req.Query["validationToken"];
        if (!string.IsNullOrEmpty(validationToken))
        {
            _logger.LogInformation("Validation token received: {ValidationToken}", validationToken);
            return new ContentResult
            {
                Content = validationToken,
                ContentType = "text/plain",
                StatusCode = StatusCodes.Status200OK
            };
        }

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<WebhookPayload>(requestBody);

        if (payload?.Value == null || payload.Value.Count == 0)
        {
            _logger.LogWarning("No valid payload received.");
            return new BadRequestResult();
        }

        // Extract the email ID from the payload
        string emailId = payload.Value.First().ResourceData.Id;

        var graphClient = _graphService.GetGraphServiceClient();

        var supportEmailId = (await graphClient.Users.Request()
            .Filter($"mail eq 'eba@ebarocks.onmicrosoft.com'")
            .Select("id")
            .GetAsync()).First().Id;

        var email = await graphClient.Users[supportEmailId].Messages[emailId]
            .Request()
            .GetAsync();

        if (email == null)
        {
            _logger.LogWarning("Email not found: {EmailId}", emailId);
            return new NotFoundResult();
        }

        // read the body of the email
        var body = email.Body.Content;
        body = System.Text.RegularExpressions.Regex.Replace(body, "<.*?>", string.Empty);
        body = System.Text.RegularExpressions.Regex.Replace(body, "<!--.*?-->", string.Empty);

        var subject = email.Subject;
        var senderEmail = email.Sender.EmailAddress.Address;
        var recipient = email.ToRecipients.FirstOrDefault()?.EmailAddress.Address;
        _logger.LogInformation("Email received: {Subject} from {Sender}", subject, senderEmail);

        // Create an EmailMessage and send it to Service Bus queue
        var emailMessage = new EmailMessage
        {
            EmailId = emailId,
            Subject = subject,
            Body = body,
            Sender = senderEmail,
            Recipient = recipient ?? "unknown"
        };

        // Get Service Bus connection string and queue name from configuration
        var serviceBusConnection = _configuration["ServiceBusConnection"];
        var queueName = _configuration["ServiceBusQueueName"];

        // Send the message to Service Bus queue
        await using var client = new ServiceBusClient(serviceBusConnection);
        await using var messageSender = client.CreateSender(queueName);

        var serviceBusMessageBody = JsonSerializer.Serialize(emailMessage, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var sbMessage = new ServiceBusMessage(serviceBusMessageBody);

        try
        {
            _logger.LogInformation("Sending email to Service Bus queue: {QueueName}", queueName);
            await messageSender.SendMessageAsync(sbMessage);
            _logger.LogInformation("Email successfully sent to Service Bus queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Service Bus queue");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }

        return new OkResult();
    }

    public class WebhookPayload
    {
        [JsonPropertyName("value")]
        public required List<WebhookValue> Value { get; set; }
    }

    public class WebhookValue
    {
        [JsonPropertyName("subscriptionId")]
        public required string SubscriptionId { get; set; }
        [JsonPropertyName("subscriptionExpirationDateTime")]
        public DateTime SubscriptionExpirationDateTime { get; set; }
        [JsonPropertyName("changeType")]
        public required string ChangeType { get; set; }
        [JsonPropertyName("resource")]
        public required string Resource { get; set; }
        [JsonPropertyName("resourceData")]
        public required ResourceData ResourceData { get; set; }
        [JsonPropertyName("clientState")]
        public required string ClientState { get; set; }
        [JsonPropertyName("tenantId")]
        public required string TenantId { get; set; }
    }

    public class ResourceData
    {
        [JsonPropertyName("@odata.type")]
        public required string ODataType { get; set; }

        [JsonPropertyName("@odata.id")]
        public required string ODataId { get; set; }

        [JsonPropertyName("@odata.etag")]
        public required string ODataEtag { get; set; }

        [JsonPropertyName("id")]
        public required string Id { get; set; }
    }
}