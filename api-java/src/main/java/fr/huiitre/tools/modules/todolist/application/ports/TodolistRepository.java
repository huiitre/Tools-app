package fr.huiitre.tools.modules.todolist.application.ports;

import java.util.List;
import java.util.Optional;

import fr.huiitre.tools.modules.todolist.domain.Todolist;

public interface TodolistRepository {
    
    void save(Long userId, Todolist todolist);

    void update(Long userId, Todolist todolist);

    void delete(Long userId, Long todolistId);

    List<Todolist> findAllByUserId(Long userId);

    Optional<Todolist> findById(Long userId, Long todolistId);
}
