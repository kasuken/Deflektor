using System.Net.Http.Headers;
using Azure.Identity;
using Microsoft.Graph;

namespace Services;

public class GraphService
{
    private string _clientID;
    private string _clientSecret;
    private string _tenantID;

    public GraphService(string clientID, string clientSecret, string tenantID)
    {
        _clientID = clientID;
        _clientSecret = clientSecret;
        _tenantID = tenantID;
    }

    public GraphServiceClient GetGraphServiceClient()
    {
        var scopes = new string[] { "https://graph.microsoft.com/.default" };

        var options = new TokenCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        };

        var clientSecretCredential = new ClientSecretCredential(
           _tenantID,
           _clientID,
           _clientSecret, options);

        var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

        return graphClient;
    }
}