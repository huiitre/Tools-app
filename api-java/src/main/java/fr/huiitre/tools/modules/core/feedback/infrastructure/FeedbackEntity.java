package fr.huiitre.tools.modules.core.feedback.infrastructure;

import java.time.LocalDateTime;

public record FeedbackEntity(
    Long id,
    Long userId,
    String userName,
    String message,
    boolean isRead,
    LocalDateTime createdAt
) {}
