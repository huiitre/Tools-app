package fr.huiitre.tools.modules.elite_dangerous.r2r.application.importer;

import java.io.IOException;

public interface RouteImporter {
    String source();
    String parse(byte[] fileContent, String fileName) throws IOException;
}
