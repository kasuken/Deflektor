using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Deflektor.Func
{
    public class ElaborateEmail
    {
        private readonly GraphService _graphService;
        private readonly DeflektorEngineService _deflektorEngineService;
        private readonly ILogger<ElaborateEmail> _logger;

        public ElaborateEmail(ILogger<ElaborateEmail> logger, GraphService graphService, DeflektorEngineService deflektorEngineService)
        {
            _logger = logger;
            _graphService = graphService;
            _deflektorEngineService = deflektorEngineService;
        }

        [Function("ElaborateEmail")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
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
            var sender = email.Sender.EmailAddress.Address;
            var recipient = email.ToRecipients.FirstOrDefault()?.EmailAddress.Address;
            _logger.LogInformation("Email received: {Subject} from {Sender}", subject, sender);

            var response = await _deflektorEngineService.ElaborateTicket(body);

            // answer to the email sender with the Graph API
            var reply = new Message
            {
                Subject = "Re from Deflektor: " + subject,
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
                            Address = sender
                        }
                    }
                }
            };

            var recipientUserId = (await graphClient.Users.Request()
                .Filter($"mail eq '{recipient}'")
                .Select("id")
                .GetAsync()).First().Id;

            await graphClient.Users[recipientUserId].Messages[emailId]
                .Reply(reply)
                .Request()
                .PostAsync();

            return new OkResult();
        }
    }

    public class WebhookPayload
    {
        [JsonPropertyName("value")]
        public List<WebhookValue> Value { get; set; }
    }

    public class WebhookValue
    {
        [JsonPropertyName("subscriptionId")]
        public string SubscriptionId { get; set; }
        public DateTime SubscriptionExpirationDateTime { get; set; }
        public string ChangeType { get; set; }
        public string Resource { get; set; }
        [JsonPropertyName("resourceData")]
        public ResourceData ResourceData { get; set; }
        public string ClientState { get; set; }
        public string TenantId { get; set; }
    }

    public class ResourceData
    {
        [JsonPropertyName("@odata.type")]
        public string ODataType { get; set; }

        [JsonPropertyName("@odata.id")]
        public string ODataId { get; set; }

        [JsonPropertyName("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

}
