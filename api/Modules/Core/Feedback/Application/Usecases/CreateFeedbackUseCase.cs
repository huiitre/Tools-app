using Tools.Api.Modules.Core.Feedback.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Feedback.Application.Usecases;

public sealed class CreateFeedbackUseCase(
    UseCaseAuthorizer authorizer,
    IFeedbackRepository feedbackRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;

    public async Task Execute(string message)
    {
        await feedbackRepository.Save(CurrentUser.UserId, message);
    }
}