using Tools.ApiCore.Modules.Users.Application;
using Tools.ApiCore.Modules.Users.Application.Usecases;
using Tools.ApiCore.Modules.Users.Infrastructure;

namespace Tools.ApiCore.Modules.Users;

// Composition du module Users : le profil utilisateur.
//
// Les moyens d'identification (mot de passe, Google, session) appartiennent au module Auth,
// pas ici : voir docs/ARCHITECTURE.md, section « Contrat de routes HTTP ».
public static class UsersModule
{
    public static IHostApplicationBuilder AddUsersModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
        builder.Services.AddScoped<GetMyProfileUseCase>();
        builder.Services.AddScoped<ListUsersUseCase>();
        builder.Services.AddScoped<SetUserGlobalRoleUseCase>();

        return builder;
    }
}
