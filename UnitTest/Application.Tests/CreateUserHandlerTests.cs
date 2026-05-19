using Application.Admin.CQRS.Commands.Users;
using Application.Admin.Mapping;
using Application.Admin.Handlers.Users;
using Application.Admin.Validators.Users;
using AutoMapper;
using UnitTest.Application.Tests;

namespace UnitTests.Application.Tests
{
    public class CreateUserHandlerTests
    {
        [Fact]
        public async Task CreateUser_Success()
        {
            
            // Arrange - use in-memory DB or mock repository
            var repo = new InMemoryUserRepository();
            var mapper = new Mapper(new MapperConfiguration(cfg => cfg.AddProfile<UserProfile>(),null));
            var validator = new CreateUserCommandValidator();
            var handler = new CreateUserHandler(repo,mapper, validator);


            var command = new CreateUserCommand("testuser", "test@example.com", "P@ssword123");


            // Act
            var result = await handler.Handle(command, CancellationToken.None);


            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }
    }
}
