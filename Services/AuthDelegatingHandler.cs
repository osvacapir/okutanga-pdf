using Microsoft.Extensions.Options;
using OlondongeApp.Configuration;

namespace OlondongeApp.Services;

/// <summary>
/// Injeta Bearer e headers de tenant nas chamadas autenticadas.
/// </summary>
public sealed class AuthDelegatingHandler : DelegatingHandler
{
    private readonly IAuthService _authService;
    private readonly IOptions<ApiOptions> _options;

    public AuthDelegatingHandler(IAuthService authService, IOptions<ApiOptions> options)
    {
        _authService = authService;
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var o = _options.Value;
        if (!string.IsNullOrWhiteSpace(o.TenantDomain))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Domain", o.TenantDomain);
        }

        if (!string.IsNullOrWhiteSpace(o.TenantSlug))
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Slug", o.TenantSlug);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
