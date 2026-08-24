using Tools.Api.Modules.Core.Feedback.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Feedback.Application.Usecases;

public sealed class UpdateFeedbackReadStatusUseCase(
    UseCaseAuthorizer authorizer,
    IFeedbackRepository feedbackRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(List<long> feedbackIds, bool isRead)
    {
        await feedbackRepository.UpdateReadStatus(feedbackIds, isRead);
    }
}