package fr.huiitre.tools.modules.core.user.domain;

import java.time.LocalDateTime;

public class User {

    private Long id;
    private final String name;
    private final String email;
    private UserType userType;
    private boolean active;
    private LocalDateTime createdAt;
    private AvatarSource avatarSource;

    public User(String name, String email, UserType userType, AvatarSource avatarSource) {
        this.name = name;
        this.email = email;
        this.userType = userType != null ? userType : UserType.HUMAN;
        this.active = false;
        this.avatarSource = avatarSource != null ? avatarSource : AvatarSource.PASSWORD;
    }

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public String getName() {
        return name;
    }

    public String getEmail() {
        return email;
    }

    public UserType getUserType() {
        return userType;
    }

    public boolean isActive() {
        return active;
    }

    public void setIsActive(boolean bool) {
        this.active = bool;
    }

    public LocalDateTime getCreatedAt() {
        return createdAt;
    }

    public void setCreatedAt(LocalDateTime createdAt) {
        this.createdAt = createdAt;
    }

    public AvatarSource getAvatarSource() {
        return avatarSource;
    }

    public void setAvatarSource(AvatarSource avatarSource) {
        this.avatarSource = avatarSource != null ? avatarSource : AvatarSource.PASSWORD;
    }
}
