package fr.huiitre.tools.modules.palworld.serverdata.application.ports;

import fr.huiitre.tools.modules.palworld.serverdata.application.view.ServerDataInventoryView;

public interface ServerInventoryQueryRepository {
    ServerDataInventoryView getCurrentInventory();
}
