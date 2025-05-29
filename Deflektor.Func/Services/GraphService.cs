using Azure.Identity;
using Deflektor.Func.Interfaces;
using Deflektor.Func.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using System.Text.Json;

namespace Deflektor.Func.Services;

/// <summary>
/// Service for Microsoft Graph API operations
/// </summary>
public class GraphService : IGraphService
{
    private readonly GraphApiSettings _settings;
    private readonly ILogger<GraphService> _logger;

    /// <summary>
    /// Initializes a new instance of the GraphService
    /// </summary>
    /// <param name="options">Graph API configuration options</param>
    /// <param name="logger">Logger instance</param>
    public GraphService(IOptions<AppSettings> options, ILogger<GraphService> logger)
    {
        _settings = options.Value.GraphApi;
        _logger = logger;
    }

    /// <inheritdoc/>
    public GraphServiceClient GetGraphServiceClient()
    {
        try
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(
               _settings.TenantId,
               _settings.ClientId,
               _settings.ClientSecret,
               options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            return graphClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Graph client");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Message> GetEmailByResourcePath(string resourcePath)
    {
        try
        {
            // Use the Graph client to directly fetch the message by ID
            var graphClient = GetGraphServiceClient();

            // Parse the resource path to extract user ID and message ID
            var segments = resourcePath.Split('/');
            if (segments.Length >= 4 && segments[0] == "users" && segments[2] == "messages")
            {
                string userId = segments[1];
                string messageId = segments[3];

                var email = await graphClient.Users[userId].Messages[messageId].Request().GetAsync();
                return email;
            }

            throw new ArgumentException($"Invalid resource path format: {resourcePath}");
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error retrieving email from Graph API with resource path: {ResourcePath}", resourcePath);
            throw;
        }
    }

    public async Task<Message> GetEmailById(string messageId)
    {
        try
        {
            // Use the Graph client to directly fetch the message by ID
            var graphClient = GetGraphServiceClient();

            var userId = (await graphClient.Users.Request()
                .Filter($"mail eq 'eba@ebarocks.onmicrosoft.com'")
                .Select("id")
                .GetAsync()).First().Id;

            var email = await graphClient.Users[userId].Messages[messageId].Request().GetAsync();
            return email;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error retrieving email from Graph API with resource path: {messageId}", messageId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ReplyToEmail(string recipientUserId, string emailId, Message reply)
    {
        try
        {
            var graphClient = GetGraphServiceClient();
            await graphClient.Users[recipientUserId].Messages[emailId]
                .Reply(reply)
                .Request()
                .PostAsync();
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error replying to email {EmailId} for user {UserId}", emailId, recipientUserId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetUserIdFromEmail(string email)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Empty email address provided for user lookup");
                return null;
            }

            var graphClient = GetGraphServiceClient();
            var users = await graphClient.Users.Request()
                .Filter($"mail eq '{email}'")
                .Select("id")
                .GetAsync();

            var user = users.FirstOrDefault();
            return user?.Id;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Error retrieving user ID for email: {Email}", email);
            return null;
        }
    }
}