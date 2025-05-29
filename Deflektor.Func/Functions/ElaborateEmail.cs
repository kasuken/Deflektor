using Deflektor.Func.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Deflektor.Func.Functions;

/// <summary>
/// Function to handle incoming email webhook notifications
/// </summary>
public class ElaborateEmail
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ElaborateEmail> _logger;

    /// <summary>
    /// Initializes a new instance of the ElaborateEmail function
    /// </summary>
    /// <param name="emailService">Email service instance</param>
    /// <param name="logger">Logger instance</param>
    public ElaborateEmail(IEmailService emailService, ILogger<ElaborateEmail> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Processes webhook notifications from Microsoft Graph for new emails
    /// </summary>
    /// <param name="req">The HTTP request</param>
    /// <returns>HTTP response</returns>
    [Function("ElaborateEmail")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("ElaborateEmail function processing a request");
        
        // Check if this is a validation request
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

        try
        {
            // Read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                _logger.LogWarning("Empty request body received");
                return new BadRequestResult();
            }

            // Process the webhook notification
            var success = await _emailService.ProcessWebhookNotification(requestBody);
            
            if (success)
            {
                return new OkResult();
            }
            else
            {
                _logger.LogWarning("Failed to process webhook notification");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook request");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}