using Application.Admin.Behavior;
using Application.Admin.DTOs;
using Application.Admin.Handlers;
using Application.Admin.Handlers.Users;
using Application.Admin.Mapping;
using Application.Admin.Validators.Users;
using Domain.Entities.Graphql;
using Domain.Interfaces;
using Domain.Interfaces.Graphql.User;
using FluentValidation;
using Infrastructure.EFModels;
using Infrastructure.Repositories.Graphql;
using Infrastructure.Repositories.Graphql.User;
using Infrastructure.Repositories.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddGraphQLService(builder.Configuration);

Log.Logger = new LoggerConfiguration()
.ReadFrom.Configuration(builder.Configuration)
.Enrich.FromLogContext()
.WriteTo.Console()
.CreateLogger();


builder.Host.UseSerilog();
builder.Services.AddAutoMapper(typeof(UserProfile).Assembly);

builder.Services.Configure<GraphQLSettings>(builder.Configuration.GetSection("GraphQL"));

builder.Services.AddHttpClient<GraphQLService>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly, typeof(CreateUserHandler).Assembly));


// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();
// DbContext (assume connection string in config)
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserInfoRepository, UserInfoRepository>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
