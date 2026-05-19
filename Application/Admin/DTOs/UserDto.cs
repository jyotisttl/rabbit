using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Admin.DTOs
{
    public record UserDto(Guid Id, string Username, string Email, DateTime CreatedAt);
}
