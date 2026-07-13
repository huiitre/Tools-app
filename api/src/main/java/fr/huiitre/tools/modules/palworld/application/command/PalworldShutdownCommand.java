package fr.huiitre.tools.modules.palworld.application.command;

public class PalworldShutdownCommand {

    private int waittime;
    private String message;

    public int getWaittime() {
        return waittime;
    }

    public void setWaittime(int waittime) {
        this.waittime = waittime;
    }

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    }
}
