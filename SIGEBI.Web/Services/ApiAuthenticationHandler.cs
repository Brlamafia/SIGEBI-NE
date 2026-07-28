using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace SIGEBI.Web.Services;

public sealed class ApiAuthenticationHandler(
    IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is not null)
        {
            var token = await context.GetTokenAsync("access_token");
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
