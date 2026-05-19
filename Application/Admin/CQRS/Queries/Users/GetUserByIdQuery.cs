using Application.Admin.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries.Users
{
    public record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;
}
