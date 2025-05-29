namespace Deflektor.Func.Interfaces;

/// <summary>
/// Interface for AI ticket processing service
/// </summary>
public interface IDeflektorEngineService
{
    /// <summary>
    /// Processes an email body and generates a support response
    /// </summary>
    /// <param name="emailBody">The plain text body of the email</param>
    /// <returns>A structured support response</returns>
    Task<TicketSupportResponse> ElaborateTicket(string emailBody);
}