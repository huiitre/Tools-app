package fr.huiitre.tools.modules.core.feedback.application.usecase;

import java.util.List;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.core.feedback.application.port.FeedbackRepository;
import fr.huiitre.tools.modules.core.feedback.infrastructure.FeedbackEntity;

@Service
public class GetAllFeedbacksUseCase implements SecuredUseCase {

    private final FeedbackRepository feedbackRepository;

    public GetAllFeedbacksUseCase(FeedbackRepository feedbackRepository) {
        this.feedbackRepository = feedbackRepository;
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.ADMIN;
    }

    public List<FeedbackEntity> execute() {
        return feedbackRepository.findAllSortedByDateDesc();
    }
}
