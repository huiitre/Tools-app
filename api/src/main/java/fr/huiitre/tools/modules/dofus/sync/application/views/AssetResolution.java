package fr.huiitre.tools.modules.dofus.sync.application.views;

public enum AssetResolution {

    X1("1x", 64),
    X2("2x", 128);

    private final String folder;
    private final int size;

    AssetResolution(String folder, int size) {
        this.folder = folder;
        this.size = size;
    }

    public String getFolder() {
        return folder;
    }

    public int getSize() {
        return size;
    }

    public static AssetResolution fromDb(String value) {
        return AssetResolution.valueOf(value);
    }
}
