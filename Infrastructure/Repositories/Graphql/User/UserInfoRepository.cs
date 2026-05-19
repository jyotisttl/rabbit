//using Application.Admin.DTOs.User;
using Domain.Interfaces.Graphql;
using Domain.Interfaces.Graphql.User;
using Domain.Model;
using Infrastructure.Graphql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Graphql.User
{
    public class UserInfoRepository(IGraphQLService graphQLService) : IUserInfoRepository
    {
        private readonly IGraphQLService _graphQLService = graphQLService;

        public async Task<UserInfoDto?> GetUserById(int userId)
        {
            var query = GraphQLQueryLoader.LoadQuery("User/GetUserInfoById.graphql");

            var result = await _graphQLService.ExecuteQueryAsync(query, new { id = userId });

            var userToken = result?["data"]?["users"]?.FirstOrDefault();
            if (userToken == null)
            {
                return null;
            }
                return userToken?.ToObject<UserInfoDto>();
            }
        }
    }
