using Domain.Entities.Graphql;
using Domain.Interfaces.Graphql;
using GraphQL.Client.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Infrastructure.Repositories.Graphql
{
    public class GraphQLService : IGraphQLService
    {
        private readonly GraphQLHttpClient _client;
        private readonly GraphQLSettings _settings;
        private readonly HttpClient _httpClient;

        public GraphQLService(HttpClient httpClient, IOptions<GraphQLSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<JObject?> ExecuteQueryAsync(string query, object? variables = null)
        {

            var request = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoint);

            request.Headers.Add("x-hasura-admin-secret", _settings.AdminSecret);

            var body = new
            {
                query = query,
                variables = variables
            };

            request.Content = new StringContent(
                JsonConvert.SerializeObject(body),
                Encoding.UTF8,
                "application/json"
                );

            var response = await _httpClient.SendAsync(request);
            var content =  await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }
    }
    
}
