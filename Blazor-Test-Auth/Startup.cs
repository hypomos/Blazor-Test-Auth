namespace Blazor_Test_Auth
{
    using System.Threading.Tasks;

    using Blazor_Test_Auth.Data;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.AzureAD.UI;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Tokens;

    // all: https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/
    // read: https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/1-WebApp-OIDC
    // sign in: https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/1-WebApp-OIDC/1-3-AnyOrgOrPersonal
    // sign out: https://github.com/Azure-Samples/active-directory-aspnetcore-webapp-openidconnect-v2/tree/master/1-WebApp-OIDC/1-6-SignOut

    // scopes: https://github.com/microsoftgraph/aspnetcore-connect-sample/tree/master/MicrosoftGraphAspNetCoreConnectSample

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                    .AddAzureAD(options => this.Configuration.Bind("AzureAd", options))
                    .AddCookie();


            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme,
                                                     options =>
                                                     {
                                                         options.TokenValidationParameters =
                                                             new TokenValidationParameters
                                                                 {
                                                                     // Instead of using the default validation (validating against a single issuer value, as we do in
                                                                     // line of business apps), we inject our own multitenant validation logic
                                                                     ValidateIssuer = false

                                                                     // If the app is meant to be accessed by entire organizations, add your issuer validation logic here.
                                                                     //IssuerValidator = (issuer, securityToken, validationParameters) => {
                                                                     //    if (myIssuerValidationLogic(issuer)) return issuer;
                                                                     //}
                                                                 };

                                                         options.Scope.Add("Files.ReadWrite.All");

                                                         options.Events = new OpenIdConnectEvents
                                                                              {
                                                                                  OnTicketReceived = context =>
                                                                                  {
                                                                                      // If your authentication logic is based on users then add your logic here
                                                                                      return Task.CompletedTask;
                                                                                  },
                                                                                  OnAuthenticationFailed = context =>
                                                                                  {
                                                                                      context
                                                                                          .Response.Redirect("/Error");
                                                                                      context
                                                                                          .HandleResponse(); // Suppress the exception
                                                                                      return Task.CompletedTask;
                                                                                  },

                                                                                  // If your application needs to authenticate single users, add your user validation below.
                                                                                  OnTokenValidated = context =>
                                                                                  {
                                                                                      return Task.CompletedTask;
                                                                                  }
                                                                              };
                                                     });

            services.AddControllersWithViews(options =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                                             .Build();

                options.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddSingleton<WeatherForecastService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            var microsoftAccountHandler = app.ApplicationServices.GetService<MicrosoftAccountHandler>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}