using Newtonsoft.Json.Linq;

namespace Domain.Interfaces.Graphql
{
    public interface IGraphQLService
    {
        Task<JObject?> ExecuteQueryAsync(string query, object? variables = null);
    }
}
