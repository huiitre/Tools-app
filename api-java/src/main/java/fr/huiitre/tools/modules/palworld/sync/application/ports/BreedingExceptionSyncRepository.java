package fr.huiitre.tools.modules.palworld.sync.application.ports;

public interface BreedingExceptionSyncRepository {

    void deleteAll();

    /** @return true si la ligne a bien été insérée, false si elle existait déjà (doublon exact ignoré). */
    boolean insert(Long parentAPalId, String parentAGenderCode, Long parentBPalId, String parentBGenderCode, Long childPalId);
}
