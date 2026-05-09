package fr.huiitre.tools.modules.riot.valorant.application.ports;

import java.util.Map;

public interface ValorantVersionProvider {
    Map<String, Object> getVersion();
}
