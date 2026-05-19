namespace Infrastructure.Graphql
{
    public static class GraphQLQueryLoader
    {
        public static string LoadQuery(string relativePath)
        {
            // Queries will be copied to output under GraphQL/
            var path = Path.Combine(AppContext.BaseDirectory, "Graphql", relativePath);

            if (!File.Exists(path))
                throw new FileNotFoundException($"GraphQL query file not found: {path}");

            return File.ReadAllText(path);
        }
    }
}
