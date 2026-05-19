using System;
using AutoMapper;
using Domain.Entities;
using Application.Admin.DTOs;
using Application.Admin.DTOs.User;

namespace Application.Admin.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDto>();
           // CreateMap<User, UserInfoDto>();
        }
    }
}
