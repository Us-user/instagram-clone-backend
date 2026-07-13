using Domain.Entities;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Mapping;
using Infrastructure.Options;
using Infrastructure.Services;
using Infrastructure.Services.Interfaces;
using Infrastructure.Validators.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>
/// Регистрация сервисов слоя Infrastructure: DbContext, ядро Identity, сквозные сервисы
/// (токены, файлы, текущий пользователь), AutoMapper и FluentValidation-валидаторы.
/// Схема JWT-аутентификации подключается в WebApi (пакет JwtBearer живёт там).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DataContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // AddIdentityCore не тянет cookie-схему аутентификации — это удобно для JWT-API.
        // Даёт UserManager/RoleManager (нужны для Seed и Account-сервиса).
        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DataContext>()
            .AddDefaultTokenProviders();

        // Параметры JWT из секции "Jwt".
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Доступ к HttpContext для чтения claim'ов текущего пользователя.
        services.AddHttpContextAccessor();

        // Сквозные сервисы.
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileService, FileService>();

        // Сервисы фич.
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IFollowingRelationShipService, FollowingRelationShipService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IChatService, ChatService>();

        // AutoMapper: профили из этой сборки.
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // FluentValidation: все валидаторы из этой сборки.
        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        return services;
    }
}
