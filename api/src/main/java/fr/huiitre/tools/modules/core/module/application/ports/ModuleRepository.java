package fr.huiitre.tools.modules.core.module.application.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.core.module.domain.Module;

public interface ModuleRepository {

    void save(Module module);

    void delete(Module module);

    void update(Module module);

    Optional<Module> findById(Long id);

    boolean existsByCode(String code);

    List<Module> findAll();
}
