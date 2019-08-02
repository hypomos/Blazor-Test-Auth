namespace Hypomos.Blazor.AuthTests.Pages
{
    using System.Security.Claims;
    using System.Threading.Tasks;

    using Hypomos.Blazor.AuthTests.Helpers;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Graph;

    public class CounterBase : ComponentBase
    {
        public CounterBase()
        {
            this.Children = new DriveItemChildrenCollectionPage();
        }

        [Inject]
        protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        protected IDriveItemChildrenCollectionPage Children { get; set; }

        protected int CurrentCount { get; set; }

        [Inject]
        protected IGraphSdkHelper GraphSdkHelper { get; set; }

        protected void IncrementCount()
        {
            this.CurrentCount++;
        }

        protected async Task TryGraph()
        {
            var state = await this.AuthenticationStateProvider.GetAuthenticationStateAsync();

            // Initialize the GraphServiceClient.
            var graphClient = this.GraphSdkHelper.GetAuthenticatedClient((ClaimsIdentity)state.User.Identity);

            this.Children = await graphClient.Drive.Root.Children.Request().GetAsync();
        }
    }
}