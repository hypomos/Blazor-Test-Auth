namespace Hypomos.Blazor.AuthTests.Extensions
{
    using System;
    using System.Threading.Tasks;

    using Hypomos.Blazor.AuthTests.Helpers;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;

    public static class AzureAdAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddAzureAd(this AuthenticationBuilder builder)
        {
            return builder.AddAzureAd(_ => { });
        }

        public static AuthenticationBuilder AddAzureAd(
            this AuthenticationBuilder builder,
            Action<AzureAdOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();
            builder.AddOpenIdConnect();
            return builder;
        }

        public class ConfigureAzureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private readonly IGraphAuthProvider authProvider;

            private readonly AzureAdOptions azureOptions;

            public ConfigureAzureOptions(IOptions<AzureAdOptions> azureOptions, IGraphAuthProvider authProvider)
            {
                this.azureOptions = azureOptions.Value;
                this.authProvider = authProvider;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.ClientId = this.azureOptions.ClientId;
                options.Authority = $"{this.authProvider.Authority}v2.0";
                options.UseTokenLifetime = true;
                options.CallbackPath = this.azureOptions.CallbackPath;
                options.RequireHttpsMetadata = false;
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                var allScopes = $"{this.azureOptions.Scopes} {this.azureOptions.GraphScopes}".Split(new[] { ' ' });
                foreach (var scope in allScopes)
                {
                    options.Scope.Add(scope);
                }

                options.TokenValidationParameters = new TokenValidationParameters
                                                        {
                                                            // Ensure that User.Identity.Name is set correctly after login
                                                            NameClaimType = "name",

                                                            // Instead of using the default validation (validating against a single issuer value, as we do in line of business apps),
                                                            // we inject our own multitenant validation logic
                                                            ValidateIssuer = false,
                                                            
                                                            SaveSigninToken = true

                                                            // If the app is meant to be accessed by entire organizations, add your issuer validation logic here.
                                                            // IssuerValidator = (issuer, securityToken, validationParameters) => {
                                                            // if (myIssuerValidationLogic(issuer)) return issuer;
                                                            // }
                                                        };

                options.Events = new OpenIdConnectEvents
                                     {
                                         OnTicketReceived = context =>
                                             {
                                                 // If your authentication logic is based on users then add your logic here
                                                 return Task.CompletedTask;
                                             },
                                         OnAuthenticationFailed = context =>
                                             {
                                                 context.Response.Redirect("/Home/Error");
                                                 context.HandleResponse(); // Suppress the exception
                                                 return Task.CompletedTask;
                                             },
                                         OnAuthorizationCodeReceived = async context =>
                                             {
                                                 var code = context.ProtocolMessage.Code;
                                                 var identifier = context.Principal
                                                     .FindFirst(Startup.ObjectIdentifierType).Value;

                                                 var result =
                                                     await this.authProvider.GetUserAccessTokenByAuthorizationCode(
                                                         code);

                                                 // Check whether the login is from the MSA tenant. 
                                                 // The sample uses this attribute to disable UI buttons for unsupported operations when the user is logged in with an MSA account.
                                                 var currentTenantId = context.Principal.FindFirst(Startup.TenantIdType)
                                                     .Value;
                                                 if (currentTenantId == "9188040d-6c67-4c5b-b112-36a304b66dad")
                                                 {
                                                     // MSA (Microsoft Account) is used to log in
                                                 }

                                                 context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                                             }

                                         // If your application needs to do authenticate single users, add your user validation below.
                                         // OnTokenValidated = context =>
                                         // {
                                         // return myUserValidationLogic(context.Ticket.Principal);
                                         // }
                                     };
            }

            public void Configure(OpenIdConnectOptions options)
            {
                this.Configure(Options.DefaultName, options);
            }

            public AzureAdOptions GetAzureAdOptions()
            {
                return this.azureOptions;
            }
        }
    }
}