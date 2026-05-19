using Domain.Entities.Graphql;
using Domain.Interfaces.Graphql;
using Infrastructure.Repositories.Graphql;

namespace WebApi
{
    public static class HasuraServiceCollectionExtension
    {
        public static IServiceCollection AddGraphQLService(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = new GraphQLSettings();
            configuration.GetSection("GraphQL").Bind(settings);

            services.AddSingleton(settings);
            services.AddHttpClient(); // Needed for Polly + GraphQL

            services.AddTransient<IGraphQLService, GraphQLService>();

            return services;
        }
    }
}
