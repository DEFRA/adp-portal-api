﻿using Microsoft.Graph;
using ADP.Portal.Api.Models;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;


namespace ADP.Portal.Core.Ado.Services
{
    internal class GraphClient : IGraphClient
    {
        private static GraphServiceClient? graphClient = null;
        private static IConfiguration? configuration = null;
        private static ADConfig serviceConnectcion; 

        public GraphClient()
        {
            configuration = new ConfigurationBuilder().SetBasePath(System.IO.Directory.GetCurrentDirectory())
                                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                                        .AddEnvironmentVariables()
                                        .Build();

            serviceConnectcion = new ADConfig();
            configuration.Bind("OpenVPN", serviceConnectcion);  
        }

        public async Task<GraphServiceClient> GetServiceClient()
        {      
            string authority = $"{serviceConnectcion.Instance}{serviceConnectcion.TenantId}";
            try
            {
                AuthenticationContext _authContext = new AuthenticationContext(authority);
                ClientCredential _clientCred = new ClientCredential(serviceConnectcion.ClientId, serviceConnectcion.ClientSecret);

                //Getting bearer token for Authentication
                AuthenticationResult _authResult = await _authContext.AcquireTokenAsync(serviceConnectcion.GraphResource, _clientCred);
                var token = _authResult.AccessToken;

                //Getting Graphclient API Endpoints
                var _authProvider = new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token.ToString());
                    return Task.FromResult(0);
                });

                string? graphAPIEndpoint = $"{serviceConnectcion.GraphResource}{serviceConnectcion.GraphResourceEndPoint}";

                // Initializing the GraphServiceClient with Authentication
                graphClient = new GraphServiceClient(graphAPIEndpoint, _authProvider);
                return graphClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            
        }
        public string GetGroupId()
        {
            return serviceConnectcion.GroupId;
        }
    }
}