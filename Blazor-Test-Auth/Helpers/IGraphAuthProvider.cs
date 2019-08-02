namespace Hypomos.Blazor.AuthTests.Helpers
{
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;

    public interface IGraphAuthProvider
    {
        string Authority { get; }

        Task<string> GetUserAccessTokenAsync(string userId);

        Task<AuthenticationResult> GetUserAccessTokenByAuthorizationCode(string authorizationCode);
    }
}