package fr.huiitre.tools.modules.elite_dangerous.r2r.domain;

public class R2rExpedition {

    private final String routeData;
    private String name;
    private final String source;

    private R2rExpedition(String routeData, String name, String source) {
        this.routeData = routeData;
        this.name = name;
        this.source = source;
    }

    public static R2rExpedition create(String routeData, String name, String source) {
        if (routeData == null || routeData.isBlank()) throw new IllegalArgumentException("R2R_ROUTE_DATA_REQUIRED");
        if (name == null || name.isBlank()) throw new IllegalArgumentException("R2R_NAME_REQUIRED");
        if (source == null || source.isBlank()) throw new IllegalArgumentException("R2R_SOURCE_REQUIRED");
        return new R2rExpedition(routeData, name, source);
    }

    public void rename(String name) {
        if (name == null || name.isBlank()) throw new IllegalArgumentException("R2R_NAME_REQUIRED");
        this.name = name;
    }

    public String getRouteData() { return routeData; }
    public String getName() { return name; }
    public String getSource() { return source; }
}
