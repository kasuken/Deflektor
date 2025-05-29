using Microsoft.Graph;

namespace Deflektor.Func.Interfaces;

/// <summary>
/// Interface for Microsoft Graph API service operations
/// </summary>
public interface IGraphService
{
    /// <summary>
    /// Gets a configured instance of GraphServiceClient
    /// </summary>
    /// <returns>An authenticated GraphServiceClient</returns>
    GraphServiceClient GetGraphServiceClient();

    /// <summary>
    /// Gets an email message by direct resource URL
    /// </summary>
    /// <param name="resourcePath">The Graph API resource path</param>
    /// <returns>The message object</returns>
    Task<Message> GetEmailByResourcePath(string resourcePath);

    Task<Message?> GetEmailById(string emailId);

    /// <summary>
    /// Sends a reply to an email
    /// </summary>
    /// <param name="recipientUserId">User ID of recipient</param>
    /// <param name="emailId">Email ID to reply to</param>
    /// <param name="reply">Reply message content</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task ReplyToEmail(string recipientUserId, string emailId, Message reply);

    /// <summary>
    /// Gets a user ID from an email address
    /// </summary>
    /// <param name="email">Email address to search</param>
    /// <returns>The user ID or null if not found</returns>
    Task<string?> GetUserIdFromEmail(string email);
}