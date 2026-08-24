namespace Tools.Api.Modules.Core.Feedback.Application.Dto;

public sealed record FeedbackDto(
    long Id,
    long UserId,
    string UserName,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);