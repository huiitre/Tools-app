using Tools.ApiCore.Modules.Mail.Application.Ports;
using Tools.ApiCore.Modules.Mail.Application.Services;
using Tools.ApiCore.Modules.Mail.Application.Usecases;
using Tools.ApiCore.Modules.Mail.Infrastructure;

namespace Tools.ApiCore.Modules.Mail;

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
