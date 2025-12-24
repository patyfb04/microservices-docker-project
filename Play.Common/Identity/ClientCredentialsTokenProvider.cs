using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Play.Common.Identity
{
    public class ClientCredentialsTokenProvider : ITokenProvider
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _sync = new(1, 1);
        private string? _cachedToken;
        private DateTime _expiresAt;
        private string? _cachedTokenEndpoint;

        public ClientCredentialsTokenProvider(IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _httpFactory = httpFactory;
            _configuration = configuration;
        }

        public async Task<string> GetTokenAsync(CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _expiresAt)
                return _cachedToken!;

            await _sync.WaitAsync(ct);
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _expiresAt)
                    return _cachedToken!;

                var authority = _configuration["Auth:Authority"] ?? throw new InvalidOperationException("Auth:Authority is not configured");
                var clientId = _configuration["Auth:ClientId"] ?? throw new InvalidOperationException("Auth:ClientId is not configured");
                var clientSecret = _configuration["Auth:ClientSecret"] ?? throw new InvalidOperationException("Auth:ClientSecret is not configured");
                var scope = _configuration["Auth:Scope"] ?? string.Empty;

                var http = _httpFactory.CreateClient();

                // Discover token endpoint if not cached
                if (string.IsNullOrEmpty(_cachedTokenEndpoint))
                {
                    var discoveryUrl = authority.TrimEnd('/') + "/.well-known/openid-configuration";
                    using var discoResponse = await http.GetAsync(discoveryUrl, ct);
                    if (!discoResponse.IsSuccessStatusCode)
                    {
                        var body = await discoResponse.Content.ReadAsStringAsync(ct);
                        throw new InvalidOperationException($"Discovery endpoint returned {(int)discoResponse.StatusCode}: {discoResponse.ReasonPhrase}. Body: {body}");
                    }

                    await using var discoStream = await discoResponse.Content.ReadAsStreamAsync(ct);
                    using var discoJson = await JsonDocument.ParseAsync(discoStream, cancellationToken: ct);
                    if (!discoJson.RootElement.TryGetProperty("token_endpoint", out var tokenEndpointElement))
                        throw new InvalidOperationException("Discovery document did not contain 'token_endpoint'.");

                    _cachedTokenEndpoint = tokenEndpointElement.GetString()
                                         ?? throw new InvalidOperationException("token_endpoint value is empty in discovery document");
                }

                // Prepare form for client_credentials
                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret
                };
                if (!string.IsNullOrEmpty(scope))
                    form["scope"] = scope;

                using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _cachedTokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(form)
                };

                using var tokenResponse = await http.SendAsync(tokenRequest, ct);
                var tokenBody = await tokenResponse.Content.ReadAsStringAsync(ct);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Token endpoint returned {(int)tokenResponse.StatusCode}: {tokenResponse.ReasonPhrase}. Body: {tokenBody}");
                }

                using var tokenJson = JsonDocument.Parse(tokenBody);
                if (!tokenJson.RootElement.TryGetProperty("access_token", out var accessTokenElement))
                    throw new InvalidOperationException("Token response did not contain 'access_token'.");

                var accessToken = accessTokenElement.GetString() ?? throw new InvalidOperationException("access_token is empty");

                int expiresInSeconds = 0;
                if (tokenJson.RootElement.TryGetProperty("expires_in", out var expiresInElement) &&
                    expiresInElement.ValueKind == JsonValueKind.Number &&
                    expiresInElement.TryGetInt32(out var parsedSeconds))
                {
                    expiresInSeconds = parsedSeconds;
                }

                _cachedToken = accessToken;
                _expiresAt = expiresInSeconds > 0
                    ? DateTime.UtcNow.AddSeconds(expiresInSeconds - 30)
                    : DateTime.UtcNow.AddMinutes(5); // fallback expiry

                return _cachedToken;
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}