package fr.huiitre.tools.modules.core.user.application.view;

import java.time.LocalDateTime;
import java.util.List;

public class UserAdminView {

    private final Long id;
    private final String email;
    private final String name;
    private final boolean active;
    private final LocalDateTime createdAt;
    private final String avatarUrl;
    private final List<Long> roles;

    public UserAdminView(Long id, String email, String name, boolean active, LocalDateTime createdAt, String avatarUrl, List<Long> roles) {
        this.id = id;
        this.email = email;
        this.name = name;
        this.active = active;
        this.createdAt = createdAt;
        this.avatarUrl = avatarUrl;
        this.roles = roles;
    }

    public Long getId() { return id; }
    public String getEmail() { return email; }
    public String getName() { return name; }
    public boolean isActive() { return active; }
    public LocalDateTime getCreatedAt() { return createdAt; }
    public String getAvatarUrl() { return avatarUrl; }
    public List<Long> getRoles() { return roles; }
}
