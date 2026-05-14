package fr.huiitre.tools.modules.core.feedback.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.feedback.application.port.FeedbackRepository;

@Service
public class BatchUpdateFeedbacksReadStatusUseCase implements SecuredUseCase {

    private final FeedbackRepository feedbackRepository;

    public BatchUpdateFeedbacksReadStatusUseCase(FeedbackRepository feedbackRepository) {
        this.feedbackRepository = feedbackRepository;
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    public void execute(List<Long> ids, boolean isRead) {
        feedbackRepository.updateReadStatus(ids, isRead);
    }
}
