package fr.huiitre.tools.modules.core.role.domain;

import java.time.LocalDateTime;

public class UserRole {
    private Long userId;
    private Long roleId;
    private final LocalDateTime createdAt;

    public UserRole(
            Long userId,
            Long roleId) {
        this.userId = userId;
        this.roleId = roleId;
        this.createdAt = LocalDateTime.now();
    }

    public Long getUserId() {
        return this.userId;
    }

    public void setUserId(Long value) {
        this.userId = value;
    }

    public Long getRoleId() {
        return this.roleId;
    }

    public void setRoleId(Long value) {
        this.roleId = value;
    }

    public LocalDateTime getCreatedAt() {
        return this.createdAt;
    }
}
