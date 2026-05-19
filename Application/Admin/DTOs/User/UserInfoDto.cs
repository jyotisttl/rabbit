using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Admin.DTOs.User
{
    public class UserInfoDto
    {
        public Guid Id { get; set; }
        public string first_name { get; set; } = null!;
        public string last_name { get; set; } = null!;
    }
}
