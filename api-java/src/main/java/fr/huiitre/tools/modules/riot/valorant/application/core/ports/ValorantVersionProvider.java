package fr.huiitre.tools.modules.riot.valorant.application.core.ports;

import java.util.Map;

public interface ValorantVersionProvider {
    Map<String, Object> getVersion();
}
