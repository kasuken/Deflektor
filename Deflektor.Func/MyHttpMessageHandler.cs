using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Deflektor.Func;

/// <summary>
/// Custom message handler for redirecting OpenAI requests to localhost
/// </summary>
public class MyHttpMessageHandler : DelegatingHandler
{
    // Remove the HttpClientHandler initialization
    public MyHttpMessageHandler()
    {
        // Don't set the inner handler here
    }

    /// <summary>
    /// Processes HTTP requests by redirecting OpenAI API requests to localhost
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase))
        {
            request.RequestUri = new Uri($"http://localhost:1234{request.RequestUri.PathAndQuery}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
