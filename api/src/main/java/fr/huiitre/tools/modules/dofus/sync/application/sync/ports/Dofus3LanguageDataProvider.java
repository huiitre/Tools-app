package fr.huiitre.tools.modules.dofus.sync.application.sync.ports;

public interface Dofus3LanguageDataProvider {

    String getString(Long stringId);

    void reload();
}
