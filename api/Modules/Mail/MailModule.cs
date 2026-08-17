using Tools.Api.Modules.Mail.Application.Ports;
using Tools.Api.Modules.Mail.Application.Services;
using Tools.Api.Modules.Mail.Application.Usecases;
using Tools.Api.Modules.Mail.Infrastructure;

namespace Tools.Api.Modules.Mail;

// Composition du module Mail : passerelle d'envoi générique.
//
// Le module ne connaît ni contenu ni règle métier — il expédie ce qu'on lui donne
// (voir docs/MAIL.md).
public static class MailModule
{
    public static IHostApplicationBuilder AddMailModule(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<SmtpMailOptions>(
            builder.Configuration.GetSection(SmtpMailOptions.SectionName));

        builder.Services.AddSingleton<IMailSender, SmtpMailSender>();
        builder.Services.AddScoped<MailService>();
        builder.Services.AddScoped<SendMailUseCase>();
        builder.Services.AddScoped<SendInternalMailUseCase>();

        return builder;
    }
}
