namespace Hypomos.Blazor.AuthTests.Helpers
{
    using System.Security.Claims;

    using Microsoft.Graph;

    public interface IGraphSdkHelper
    {
        GraphServiceClient GetAuthenticatedClient(ClaimsIdentity userIdentity);
    }
}