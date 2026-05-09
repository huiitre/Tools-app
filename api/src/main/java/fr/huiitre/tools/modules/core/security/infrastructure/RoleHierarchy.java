package fr.huiitre.tools.modules.core.security.infrastructure;

import java.util.EnumMap;
import java.util.Map;

import fr.huiitre.tools.modules.core.role.domain.RoleCode;

public final class RoleHierarchy {

    private static final Map<RoleCode, Integer> LEVELS = new EnumMap<>(RoleCode.class);

    static {
        LEVELS.put(RoleCode.READ_ONLY, 1);
        LEVELS.put(RoleCode.USER, 2);
        LEVELS.put(RoleCode.MODERATOR, 3);
        LEVELS.put(RoleCode.ADMIN, 5);
        LEVELS.put(RoleCode.TECH, 4);
        LEVELS.put(RoleCode.OWNER, 6);
    }

    private RoleHierarchy() {
    }

    public static boolean hasAtLeast(RoleCode actual, RoleCode required) {
        return LEVELS.get(actual) >= LEVELS.get(required);
    }
}
