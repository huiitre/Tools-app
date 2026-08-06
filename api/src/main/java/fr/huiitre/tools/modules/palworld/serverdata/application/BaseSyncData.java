package fr.huiitre.tools.modules.palworld.serverdata.application;

import java.util.UUID;

public class BaseSyncData {

    private final UUID baseId;
    private final UUID guildId;
    private final Double positionX;
    private final Double positionY;
    private final Double positionZ;
    private final Double rotationX;
    private final Double rotationY;
    private final Double rotationZ;
    private final Double rotationW;
    private final Double areaRange;

    public BaseSyncData(UUID baseId, UUID guildId, Double positionX, Double positionY, Double positionZ,
            Double rotationX, Double rotationY, Double rotationZ, Double rotationW, Double areaRange) {
        this.baseId = baseId;
        this.guildId = guildId;
        this.positionX = positionX;
        this.positionY = positionY;
        this.positionZ = positionZ;
        this.rotationX = rotationX;
        this.rotationY = rotationY;
        this.rotationZ = rotationZ;
        this.rotationW = rotationW;
        this.areaRange = areaRange;
    }

    public UUID getBaseId() { return baseId; }
    public UUID getGuildId() { return guildId; }
    public Double getPositionX() { return positionX; }
    public Double getPositionY() { return positionY; }
    public Double getPositionZ() { return positionZ; }
    public Double getRotationX() { return rotationX; }
    public Double getRotationY() { return rotationY; }
    public Double getRotationZ() { return rotationZ; }
    public Double getRotationW() { return rotationW; }
    public Double getAreaRange() { return areaRange; }
}
