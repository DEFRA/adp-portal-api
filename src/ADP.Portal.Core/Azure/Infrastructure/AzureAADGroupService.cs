using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.VisualStudio.Services.Users;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace ADP.Portal.Core.Azure.Infrastructure
{
    public class AzureAADGroupService : IAzureAADGroupService
    {
        private readonly GraphServiceClient graphServiceClient;

        public AzureAADGroupService(GraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }


        public async Task<string?> GetUserIdAsync(string userPrincipalName)
        {
            try
            {
                var user = await graphServiceClient.Users[userPrincipalName].GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = ["Id"];
                });

                return user?.Id;
            }
            catch (ODataError odataException)
            {
                if (odataException.ResponseStatusCode == 404)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<bool> ExsistingMemberAsync(Guid groupId, string userPrincipalName)
        {
            var exsistingMember = await graphServiceClient.Groups[groupId.ToString()].Members.GraphUser.GetAsync((requestConfiguration) =>
                   {
                       requestConfiguration.QueryParameters.Count = true;
                       requestConfiguration.QueryParameters.Filter = $"userPrincipalName eq '{userPrincipalName}'";
                       requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                   });

            if (exsistingMember?.Value != null && exsistingMember.Value.Count == 0)
            {
                return false;
            }

            return true;
        }


        public async Task<bool> AddToAADGroupAsync(Guid groupId, string userId)
        {
            var requestBody = new ReferenceCreate
            {
                OdataId = $"https://graph.microsoft.com/beta/directoryObjects/{userId}",
            };

            await graphServiceClient.Groups[groupId.ToString()].Members.Ref.PostAsync(requestBody);

            return true;
        }
    }
}
