using Tools.Api.Modules.Core.Feedback.Application.Dto;

namespace Tools.Api.Modules.Core.Feedback.Application.Ports;

public interface IFeedbackRepository
{
    Task Save(long userId, string message);
    Task<List<FeedbackDto>> FindAllSortedByDateDesc();
    Task DeleteByIds(List<long> ids);
    Task UpdateReadStatus(List<long> ids, bool isRead);
}