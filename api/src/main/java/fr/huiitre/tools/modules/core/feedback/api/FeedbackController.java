package fr.huiitre.tools.modules.core.feedback.api;

import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;

import fr.huiitre.tools.modules.core.feedback.application.usecase.BatchDeleteFeedbacksUseCase;
import fr.huiitre.tools.modules.core.feedback.application.usecase.BatchUpdateFeedbacksReadStatusUseCase;
import fr.huiitre.tools.modules.core.feedback.application.usecase.CreateFeedbackUseCase;
import fr.huiitre.tools.modules.core.feedback.application.usecase.GetAllFeedbacksUseCase;
import fr.huiitre.tools.modules.core.feedback.infrastructure.FeedbackEntity;

@RestController
@RequestMapping("/feedbacks")
public class FeedbackController {

    private final CreateFeedbackUseCase createFeedbackUseCase;
    private final GetAllFeedbacksUseCase getAllFeedbacksUseCase;
    private final BatchDeleteFeedbacksUseCase batchDeleteFeedbacksUseCase;
    private final BatchUpdateFeedbacksReadStatusUseCase batchUpdateFeedbacksReadStatusUseCase;

    public FeedbackController(
            CreateFeedbackUseCase createFeedbackUseCase,
            GetAllFeedbacksUseCase getAllFeedbacksUseCase,
            BatchDeleteFeedbacksUseCase batchDeleteFeedbacksUseCase,
            BatchUpdateFeedbacksReadStatusUseCase batchUpdateFeedbacksReadStatusUseCase) {
        this.createFeedbackUseCase = createFeedbackUseCase;
        this.getAllFeedbacksUseCase = getAllFeedbacksUseCase;
        this.batchDeleteFeedbacksUseCase = batchDeleteFeedbacksUseCase;
        this.batchUpdateFeedbacksReadStatusUseCase = batchUpdateFeedbacksReadStatusUseCase;
    }

    @PostMapping
    @ResponseStatus(HttpStatus.CREATED)
    public void createFeedback(@RequestBody CreateFeedbackRequest request) {
        createFeedbackUseCase.execute(request.message());
    }

    @GetMapping("/admin")
    public List<FeedbackEntity> getAllFeedbacks() {
        return getAllFeedbacksUseCase.execute();
    }

    @DeleteMapping("/admin")
    public void deleteFeedbacks(@RequestBody BatchDeleteRequest request) {
        batchDeleteFeedbacksUseCase.execute(request.ids());
    }

    @PatchMapping("/admin/read-status")
    public void updateReadStatus(@RequestBody UpdateReadStatusRequest request) {
        batchUpdateFeedbacksReadStatusUseCase.execute(request.ids(), request.isRead());
    }

    // DTOs
    public record CreateFeedbackRequest(String message) {}
    public record BatchDeleteRequest(List<Long> ids) {}
    public record UpdateReadStatusRequest(List<Long> ids, boolean isRead) {}
}
