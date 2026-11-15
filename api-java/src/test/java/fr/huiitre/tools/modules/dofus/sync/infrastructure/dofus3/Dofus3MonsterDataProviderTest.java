package fr.huiitre.tools.modules.dofus.sync.infrastructure.dofus3;

import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

// Valeurs réelles de m_flags relevées dans monsters.json (Dofus 3, 15/11/2026).
class Dofus3MonsterDataProviderTest {

    @Test
    void bossesAreDetected() {
        assertTrue(Dofus3MonsterDataProvider.isBoss(139205));  // Bouftou Royal (147), Dragon Cochon (113)
        assertTrue(Dofus3MonsterDataProvider.isBoss(139237));  // boss de donjon (232)
    }

    @Test
    void regularMonstersAreNotBosses() {
        assertFalse(Dofus3MonsterDataProvider.isBoss(171985)); // valeur la plus fréquente (2 041 monstres)
        assertFalse(Dofus3MonsterDataProvider.isBoss(139201)); // même famille que le Bouftou Royal, sans le bit boss
        assertFalse(Dofus3MonsterDataProvider.isBoss(0));
    }

    @Test
    void archmonsterBitIsNotTheBossBit() {
        assertFalse(Dofus3MonsterDataProvider.isBoss(8));      // bit 3 seul : archimonstre
        assertTrue(Dofus3MonsterDataProvider.isBoss(4));       // bit 2 seul : boss
    }
}
