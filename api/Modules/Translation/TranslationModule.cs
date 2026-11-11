using Microsoft.Extensions.Options;
using Tools.Api.Modules.Translation.Application.Ports;
using Tools.Api.Modules.Translation.Application.Services;
using Tools.Api.Modules.Translation.Application.Usecases;
using Tools.Api.Modules.Translation.Infrastructure.LibreTranslate;

namespace Tools.Api.Modules.Translation;

public static class TranslationModule
{
    public static IHostApplicationBuilder AddTranslationModule(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<LibreTranslateOptions>(
            builder.Configuration.GetSection(LibreTranslateOptions.SectionName));

        // Le moteur est choisi ici : brancher un autre fournisseur ne touche ni le service ni les use cases.
        builder.Services.AddHttpClient<ITranslationProvider, LibreTranslateProvider>((services, client) =>
        {
            var options = services.GetRequiredService<IOptions<LibreTranslateOptions>>().Value;
            client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        builder.Services.AddScoped<TranslationService>();
        builder.Services.AddScoped<TranslateUseCase>();
        builder.Services.AddScoped<ListTranslationLanguagesUseCase>();
        builder.Services.AddScoped<DetectLanguageUseCase>();

        return builder;
    }
}
