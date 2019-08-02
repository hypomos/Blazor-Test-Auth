namespace Hypomos.Blazor.AuthTests.Helpers
{
    using System;
    using System.Threading.Tasks;

    using Hypomos.Blazor.AuthTests.Extensions;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;

    public class GraphAuthProvider : IGraphAuthProvider
    {
        private readonly IConfidentialClientApplication app;

        private readonly string[] scopes;

        public GraphAuthProvider(IConfiguration configuration, ILogger<GraphAuthProvider> logger)
        {
            var azureOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", azureOptions);

            // More info about MSAL Client Applications: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-Applications
            this.app = ConfidentialClientApplicationBuilder
                .Create(azureOptions.ClientId)
                .WithClientSecret(azureOptions.ClientSecret)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                .WithRedirectUri(azureOptions.BaseUrl + azureOptions.CallbackPath)
                .WithLogging((level, message, pii) => logger.LogInformation(message))
                .Build();

            this.Authority = this.app.Authority;
            this.scopes = azureOptions.GraphScopes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string Authority { get; }

        // Gets an access token. First tries to get the access token from the token cache.
        // Using password (secret) to authenticate. Production apps should use a certificate.
        public async Task<string> GetUserAccessTokenAsync(string userId)
        {
            var account = await this.app.GetAccountAsync(userId);
            if (account == null)
            {
                throw new ServiceException(
                    new Error
                    {
                        Code = "TokenNotFound",
                        Message = "User not found in token cache. Maybe the server was restarted."
                    });
            }

            try
            {
                var result = await this.app.AcquireTokenSilent(this.scopes, account).ExecuteAsync();
                return result.AccessToken;
            }

            // Unable to retrieve the access token silently.
            catch (Exception)
            {
                throw new ServiceException(
                    new Error
                    {
                        Code = GraphErrorCode.AuthenticationFailure.ToString(),
                        Message = "Caller needs to authenticate. Unable to retrieve the access token silently."
                    });
            }
        }

        public async Task<AuthenticationResult> GetUserAccessTokenByAuthorizationCode(string authorizationCode)
        {
            return await this.app.AcquireTokenByAuthorizationCode(this.scopes, authorizationCode).ExecuteAsync();
        }
    }
}