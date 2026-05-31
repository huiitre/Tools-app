package fr.huiitre.tools.modules.elite_dangerous.r2r.application.command;

import java.util.List;

public class UpdateR2rProgressCommand {

    private int currentSystemIndex;
    private List<Long> currentBodiesDone;

    public int getCurrentSystemIndex() { return currentSystemIndex; }
    public List<Long> getCurrentBodiesDone() { return currentBodiesDone; }
}
