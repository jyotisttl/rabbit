using Application.Admin.DTOs;
using MediatR;

namespace Application.Admin.CQRS.Commands.Users
{
    public record CreateUserCommand(string Username, string Email, string Password) : IRequest<UserDto>;
}
