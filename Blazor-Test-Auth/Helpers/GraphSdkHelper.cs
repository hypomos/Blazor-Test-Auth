namespace Hypomos.Blazor.AuthTests.Helpers
{
    using System.Net.Http.Headers;
    using System.Security.Claims;

    using Microsoft.Graph;

    public class GraphSdkHelper : IGraphSdkHelper
    {
        private readonly IGraphAuthProvider authProvider;

        private GraphServiceClient graphClient;

        public GraphSdkHelper(IGraphAuthProvider authProvider)
        {
            this.authProvider = authProvider;
        }

        // Get an authenticated Microsoft Graph Service client.
        public GraphServiceClient GetAuthenticatedClient(ClaimsIdentity userIdentity)
        {
            this.graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async requestMessage =>
                        {
                            // Get user's id for token cache.
                            var identifier = userIdentity.FindFirst(Startup.ObjectIdentifierType)?.Value + "."
                                                                                                         + userIdentity
                                                                                                             .FindFirst(
                                                                                                                 Startup
                                                                                                                     .TenantIdType)
                                                                                                             ?.Value;

                            var accessToken = await this.authProvider.GetUserAccessTokenAsync(identifier);

                            // Append the access token to the request
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        }));

            return this.graphClient;
        }
    }
}