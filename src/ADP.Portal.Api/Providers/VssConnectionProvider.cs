using ADP.Portal.Api.Config;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

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

        public async Task<VssConnection> GetConnectionAsync()
        {
            VssConnection connection;

            if (adoConfig.UsePatToken)
            {
                var patToken = await GetPatTokenAsync(keyValutName, adoConfig);
                connection = new VssConnection(new Uri(adoConfig.OrganizationUrl), new VssBasicCredential(string.Empty, patToken));
            }
            else
            {
                var accessToken = await GetAccessTokenAsync(azureDevOpsScope);
                connection = new VssConnection(new Uri(adoConfig.OrganizationUrl), new VssOAuthAccessTokenCredential(accessToken));
            }

            return connection;
        }

        private async Task<string> GetPatTokenAsync(string keyValutName, AdoConfig adoConfig)
        {
            var patToken = adoConfig.PatToken;

            if (string.IsNullOrEmpty(patToken))
            {
                if (string.IsNullOrEmpty(keyValutName))
                    throw new Exception("Key Vault Name is not set");

                if (string.IsNullOrEmpty(adoConfig.PatTokenSecretName))
                    throw new Exception("Personal Access Token Secret Name is not set");

                var secretClient = new SecretClient(new Uri(keyValutName), new DefaultAzureCredential());
                var secret = await secretClient.GetSecretAsync(adoConfig.PatTokenSecretName);
                patToken = secret.Value.Value;

                if (string.IsNullOrEmpty(patToken))
                {
                    throw new Exception("Personal Access Token is not set in the Key Vault");
                }
            }

            return patToken;
        }

        private async Task<string> GetAccessTokenAsync(string azureDevOpsScope)
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { azureDevOpsScope }));
            return token.Token;
        }
    }
}
