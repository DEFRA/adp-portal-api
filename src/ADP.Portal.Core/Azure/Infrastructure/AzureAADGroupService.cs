using ADP.Portal.Core.Ado.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace ADP.Portal.Core.Azure.Infrastructure
{
    public class AzureAADGroupService : IAzureAADGroupService
    {
        private readonly ILogger<AzureAADGroupService> logger;
        private readonly GraphServiceClient graphServiceClient;

        public AzureAADGroupService(GraphServiceClient graphServiceClient, ILogger<AzureAADGroupService> logger)
        {
            this.graphServiceClient = graphServiceClient;
            this.logger = logger;
        }

        public async Task<bool> AddToAADGroupAsync(Guid groupId, string userPrincipalName)
        {
            var result = await graphServiceClient.Users[userPrincipalName].GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Select = new string[] { "Id" };
            });
            
            if (result != null)
            {
                JObject obj = JObject.Parse(JsonSerializer.Serialize(result));
                string? objetcId = (string?)obj["Id"];

                var requestBody = new ReferenceCreate
                {
                    OdataId = "https://graph.microsoft.com/beta/directoryObjects/" + "{" + objetcId + "}",
                };

                await graphServiceClient.Groups[groupId.ToString()].Members.Ref.PostAsync(requestBody);
            }
            else
            {
                logger.LogWarning("User {userPrincipalName} does not exist", userPrincipalName);
                return false;
            }

            return true;
        }
    }
}
