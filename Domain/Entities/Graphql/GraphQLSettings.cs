using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Graphql
{
    public class GraphQLSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string? AdminSecret { get; set; } // Optional: JWT or Hasura admin secret
    }
}
