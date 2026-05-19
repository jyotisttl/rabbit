using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using MediatR;
using Application.Admin.CQRS.Commands.Users;
using Application.Admin.DTOs;

namespace Application.Admin.Handlers.Users
{
   
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IUserRepository _repo;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserCommand> _validator;


        public CreateUserHandler(IUserRepository repo, IMapper mapper, IValidator<CreateUserCommand> validator)
        {
            _repo = repo;
            _mapper = mapper;
            _validator = validator;
        }


        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(request, cancellationToken);


            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = request.Password,
                CreatedAt = DateTime.UtcNow
            };


            await _repo.AddAsync(user, cancellationToken);


            return _mapper.Map<UserDto>(user);
        }
    }
}
