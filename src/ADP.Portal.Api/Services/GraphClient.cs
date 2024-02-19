using Microsoft.Graph;
using ADP.Portal.Api.Models;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;


namespace ADP.Portal.Core.Ado.Services
{
    internal class GraphClient : IGraphClient
    {
        private static GraphServiceClient? graphClient;
        private static IConfiguration? configuration;
        private static string? clientId;
        private static string? clientSecret;
        private static string? tenantId;
        private static string? aadInstance;
        private static string? graphResource;
        private static string? graphAPIEndpoint;
        private static string? authority;
        private string? groupId;

        public GraphClient()
        {
            configuration = new ConfigurationBuilder().SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                                        .AddEnvironmentVariables()
                                        .Build();

            var serviceConnectcion = new ADConfig();
            configuration.Bind("OpenVPN", serviceConnectcion);
            clientId = serviceConnectcion.ClientId;
            clientSecret = serviceConnectcion.ClientSecret;
            tenantId = serviceConnectcion.TenantId;
            aadInstance = serviceConnectcion.Instance;
            graphResource = serviceConnectcion.GraphResource;
            graphAPIEndpoint = $"{serviceConnectcion.GraphResource}{serviceConnectcion.GraphResourceEndPoint}";
            authority = $"{aadInstance}{tenantId}";
            groupId = serviceConnectcion.GroupId;
        }

        public async Task<GraphServiceClient> GetServiceClient()
        {
            AuthenticationContext _authContext = new AuthenticationContext(authority);
            ClientCredential _clientCred = new ClientCredential(clientId, clientSecret);

            //Getting bearer token for Authentication
            AuthenticationResult _authResult = await _authContext.AcquireTokenAsync(graphResource, _clientCred);
            var token = _authResult.AccessToken;

            //Getting Graphclient API Endpoints
            var _authProvider = new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token.ToString());
                return Task.FromResult(0);
            });

            // Initializing the GraphServiceClient with Authentication
            graphClient = new GraphServiceClient(graphAPIEndpoint, _authProvider);
            return graphClient;
        }
        public string? GetGroupId()
        {
            return groupId;
        }
    }
}
