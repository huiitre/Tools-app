package fr.huiitre.tools.modules.dofus.pricing.application.ports;

import java.util.Optional;

public interface CaptureAccountRepository {

    Optional<Long> findUserIdByEmail(String email);
}
