package fr.huiitre.tools.modules.palworld.sync.application;

public class WorkPrioritySyncData {

    private final String code;
    private final String name;
    private final String iconUrl;
    private final String workSuitabilitySlug;
    private final int priority;

    public WorkPrioritySyncData(String code, String name, String iconUrl, String workSuitabilitySlug, int priority) {
        this.code = code;
        this.name = name;
        this.iconUrl = iconUrl;
        this.workSuitabilitySlug = workSuitabilitySlug;
        this.priority = priority;
    }

    public String getCode() { return code; }
    public String getName() { return name; }
    public String getIconUrl() { return iconUrl; }
    public String getWorkSuitabilitySlug() { return workSuitabilitySlug; }
    public int getPriority() { return priority; }
}
