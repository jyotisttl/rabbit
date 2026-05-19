using AutoMapper;
using Application.Admin.DTOs;
using Domain.Interfaces;
using MediatR;
using Application.Queries.Users;

namespace Application.Admin.Handlers.Users
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IUserRepository _repo;
        private readonly IMapper _mapper;


        public GetUserByIdHandler(IUserRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }


        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (user == null) return null;
            return _mapper.Map<UserDto>(user);
        }
    }
}
