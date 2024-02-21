// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using ADP.Portal.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;

namespace ADP.Portal.Core.Ado.Services
{
    public class UserService : IUserService
    {
        private readonly IGraphClient _graphClient;

        public UserService(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        public async Task<string?> AddOpenVPNUser([FromBody] string userPrincipalName)
        {
            string? objetcId = null;
            GraphServiceClient client = await _graphClient.GetServiceClient();
            try
            {
                var result = await client
                        .Users[userPrincipalName]
                        .Request()
                        .Select("id")
                        .GetAsync();

                if (result != null)
                {
                    JObject obj = JObject.Parse(JsonSerializer.Serialize(result));
                    objetcId = (string?)obj["id"];

                    var directoryObject = new DirectoryObject
                    {
                        Id = objetcId
                    };

                    await client.Groups[_graphClient.GetGroupId()].Members.References
                        .Request()
                        .AddAsync(directoryObject);

                    return objetcId;
                }
                else
                {
                    return null;
                }
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {                    
                    Console.WriteLine(ex.Message);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            
        }
    }
}
