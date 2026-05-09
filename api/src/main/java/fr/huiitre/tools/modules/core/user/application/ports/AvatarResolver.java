package fr.huiitre.tools.modules.core.user.application.ports;

import fr.huiitre.tools.modules.core.user.domain.User;

public interface AvatarResolver {

    String resolve(User user);
}
