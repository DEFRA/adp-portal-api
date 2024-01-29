using ADP.Portal.Api.Config;
using ADP.Portal.Core.Ado.Infrastructure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;

namespace ADP.Portal.Api.Providers
{
    public class VssConnectionProvider
    {
        private readonly string keyValutName;
        private readonly AdoConfig adoConfig;
        private const string azureDevOpsScope = "https://app.vssps.visualstudio.com/.default";

        public VssConnectionProvider(string keyValutName, AdoConfig adoConfig)
        {
            this.keyValutName = keyValutName;
            this.adoConfig = adoConfig;
        }

        public async Task<IVssConnection> GetConnectionAsync()
        {
            IVssConnection connection;

            if (adoConfig.UsePatToken)
            {
                var patToken = await GetPatTokenAsync(keyValutName, adoConfig);
                connection = new VssConnectionWrapper(new Uri(adoConfig.OrganizationUrl), new VssBasicCredential(string.Empty, patToken));
            }
            else
            {
                var accessToken = await GetAccessTokenAsync(azureDevOpsScope);
                connection = new VssConnectionWrapper(new Uri(adoConfig.OrganizationUrl), new VssOAuthAccessTokenCredential(accessToken));
            }

            return connection;
        }

        private static async Task<string> GetPatTokenAsync(string keyValutName, AdoConfig adoConfig)
        {
            var patToken = adoConfig.PatToken;

            if (string.IsNullOrEmpty(patToken))
            {
                var secretClient = new SecretClient(new Uri(keyValutName), new DefaultAzureCredential());
                var secret = await secretClient.GetSecretAsync(adoConfig.PatTokenSecretName);
                patToken = secret.Value.Value;
            }

            return patToken;
        }

        private static async Task<string> GetAccessTokenAsync(string azureDevOpsScope)
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { azureDevOpsScope }));
            return token.Token;
        }
    }
}
