package fr.huiitre.tools.modules.dofus.almanax.application.ports;

import java.time.LocalDate;
import java.util.List;

public interface AlmanaxSubscriptionRepository {

    void add(Long userId, Long almanaxId, LocalDate date);

    void remove(Long userId, Long almanaxId);

    boolean existsByUserIdAndAlmanaxId(Long userId, Long almanaxId);

    List<Long> findAlmanaxIdsByUserId(Long userId);

    List<Long> findUserIdsByAlmanaxId(Long almanaxId);
}
