using Tools.Api.Modules.Core.Feedback.Application.Dto;
using Tools.Api.Modules.Core.Feedback.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Feedback.Application.Usecases;

public sealed class GetAllFeedbacksUseCase(
    IFeedbackRepository feedbackRepository,
    UseCaseAuthorizer authoriser
) : SecuredUseCase(authoriser)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<List<FeedbackDto>> Execute()
    {
        return await feedbackRepository.FindAllSortedByDateDesc();
    }
}