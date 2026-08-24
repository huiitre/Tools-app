using Tools.Api.Modules.Core.Feedback.Application.Ports;
using Tools.Api.Modules.Core.Feedback.Application.Usecases;
using Tools.Api.Modules.Core.Feedback.Infrastructure;

namespace Tools.Api.Modules.Core.Feedback;

public static class FeedbackModule
{
    public static IHostApplicationBuilder AddFeedbackModule(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IFeedbackRepository, PostgresFeedbackRepository>();
        builder.Services.AddScoped<CreateFeedbackUseCase>();
        builder.Services.AddScoped<GetAllFeedbacksUseCase>();
        builder.Services.AddScoped<DeleteFeedbacksUseCase>();
        builder.Services.AddScoped<UpdateFeedbackReadStatusUseCase>();

        return builder;
    }
}
