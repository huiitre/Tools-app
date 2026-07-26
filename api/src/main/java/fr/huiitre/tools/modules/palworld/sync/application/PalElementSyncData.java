package fr.huiitre.tools.modules.palworld.sync.application;

public class PalElementSyncData {

    private final String externalCode;
    private final int sortOrder;

    public PalElementSyncData(String externalCode, int sortOrder) {
        this.externalCode = externalCode;
        this.sortOrder = sortOrder;
    }

    public String getExternalCode() { return externalCode; }
    public int getSortOrder() { return sortOrder; }
}
