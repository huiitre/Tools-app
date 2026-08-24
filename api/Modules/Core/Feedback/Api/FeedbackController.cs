using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Feedback.Application;
using Tools.Api.Modules.Core.Feedback.Application.Dto;
using Tools.Api.Modules.Core.Feedback.Application.Usecases;

namespace Tools.Api.Modules.Core.Feedback.Api;


[ApiController]
[Route("feedbacks")]
public class FeedbackController : ControllerBase
{
    [HttpGet("admin")]
    public Task<List<FeedbackDto>> GetAllFeedbacks(
        [FromServices] GetAllFeedbacksUseCase getAllFeedbacksUseCase
    )
    {
        return getAllFeedbacksUseCase.Execute();
    }

    [HttpPost]
    public async Task<ActionResult> CreateFeedback(
        [FromBody] CreateFeedbackRequest request,
        [FromServices] CreateFeedbackUseCase createFeedbackUseCase
    )
    {
        await createFeedbackUseCase.Execute(request.Message);
        return Created();
    }

    [HttpDelete("admin")]
    public async Task<ActionResult> DeleteFeedbacks(
        [FromBody] BatchDeleteRequest request,
        [FromServices] DeleteFeedbacksUseCase deleteFeedbacksUseCase
    )
    {
        await deleteFeedbacksUseCase.Execute(request.FeedbackIds);
        return NoContent();
    }

    [HttpPatch("admin/read-status")]
    public async Task<ActionResult> UpdateReadStatus(
        [FromBody] UpdateReadStatusRequest request,
        [FromServices] UpdateFeedbackReadStatusUseCase updateFeedbackReadStatusUseCase
    )
    {
        await updateFeedbackReadStatusUseCase.Execute(request.Ids, request.IsRead);
        return NoContent();
    }
}

public sealed record CreateFeedbackRequest(string Message);
public sealed record BatchDeleteRequest(List<long> FeedbackIds);
public sealed record UpdateReadStatusRequest(List<long> Ids, bool IsRead);