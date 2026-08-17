package fr.huiitre.tools.modules.elite_dangerous.r2r.application.command;

public class ImportR2rExpeditionCommand {

    private final byte[] fileContent;
    private final String fileName;
    private final String source;
    private final String name;

    public ImportR2rExpeditionCommand(byte[] fileContent, String fileName, String source, String name) {
        this.fileContent = fileContent;
        this.fileName = fileName;
        this.source = source;
        this.name = name;
    }

    public byte[] getFileContent() { return fileContent; }
    public String getFileName() { return fileName; }
    public String getSource() { return source; }
    public String getName() { return name; }
}
