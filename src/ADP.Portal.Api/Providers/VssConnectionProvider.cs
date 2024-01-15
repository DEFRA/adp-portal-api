using ADP.Portal.Api.Config;
using Azure.Identity;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADP.Portal.Api.Providers
{
    public class VssConnectionProvider
    {
        private readonly IHostEnvironment env;
        private readonly AdoConfig adoConfig;
        private const string azureDevOpsScope = "https://app.vssps.visualstudio.com/.default";

        public VssConnectionProvider(IHostEnvironment env, AdoConfig adoConfig)
        {
            this.env = env;
            this.adoConfig = adoConfig;
        }

        public async Task<VssConnection> GetConnectionAsync()
        {
            string accessToken;

            if (env.IsDevelopment())
            {
                var clientId = adoConfig.AzureAd.ClientId;
                var clientSecret = adoConfig.AzureAd.ClientSecret;
                var tenantId = adoConfig.AzureAd.TenantId;

                var app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();

                var result = await app.AcquireTokenForClient(new[] { azureDevOpsScope })
                    .ExecuteAsync();

                accessToken = result.AccessToken;
            }
            else
            {
                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { azureDevOpsScope }));
                accessToken = token.Token;
            }

            VssConnection connection = new VssConnection(new Uri(adoConfig.OrganizationUrl), new VssBasicCredential(string.Empty, accessToken));
            return connection;
        }
    }
}
