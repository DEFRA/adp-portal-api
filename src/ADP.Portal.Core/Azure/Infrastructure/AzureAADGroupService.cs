using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json;

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
            var result = await graphServiceClient.Groups[groupId.ToString()].Members.GraphUser.GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Count = true;
                requestConfiguration.QueryParameters.Search = "\"userPrincipalName:" + userPrincipalName + "\"";
                requestConfiguration.QueryParameters.Filter = "userPrincipalName eq " + "'" + userPrincipalName + "'";
                requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
            });

            if(result != null && 0 == result.OdataCount )
            {
                var user = await graphServiceClient.Users[userPrincipalName].GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[] { "Id" };
                });

                if (user != null)
                {
                    var requestBody = new ReferenceCreate
                    {
                        OdataId = "https://graph.microsoft.com/beta/directoryObjects/" + "{" + user.Id + "}",
                    };

                    await graphServiceClient.Groups[groupId.ToString()].Members.Ref.PostAsync(requestBody);
                }
                else
                {
                    logger.LogWarning("User {userPrincipalName} does not exist", userPrincipalName);
                    return false;
                }
            }
            else
            {
                logger.LogWarning("User {userPrincipalName} already exist", userPrincipalName);
                return false;
            } 
            return true;
        }
    }
}
