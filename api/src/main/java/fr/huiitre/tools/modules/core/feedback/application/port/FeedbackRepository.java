package fr.huiitre.tools.modules.core.feedback.application.port;

import java.util.List;
import fr.huiitre.tools.modules.core.feedback.infrastructure.FeedbackEntity;

public interface FeedbackRepository {
    void save(Long userId, String message);
    List<FeedbackEntity> findAllSortedByDateDesc();
    void deleteByIds(List<Long> ids);
    void updateReadStatus(List<Long> ids, boolean isRead);
}
