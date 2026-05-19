using Domain.Interfaces.Graphql.User;
using Domain.Model;
using Graphql;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserDetailsController(IUserInfoRepository userRepository) : ControllerBase
    {
        private readonly IUserInfoRepository _userRepository = userRepository;

        /// <summary>
        /// Retrieves details of a specific user details by its ID.
        /// </summary>
        /// <param name="userId">Unique identifier of the user details.</param>
        /// <returns>User details if found; otherwise, an error response.</returns>
        [HttpGet("{userId}", Name = "GetUserById")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var entity = await _userRepository.GetUserById(userId);

            if (entity == null)
            {
                return Ok(ApiResponse<bool>.ErrorResponse("User Details not found", HttpStatusCode.OK, null));
            }

            return Ok(ApiResponse<UserInfoDto>.SuccessResponse(
                entity,
                HttpStatusCode.OK,
                "User details retrieved successfully"
            ));
        }
    }
}
