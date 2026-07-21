package fr.huiitre.tools.modules.palworld.domain;

public class PalworldCoord {

    private static final double TRANSL_X = 123888;
    private static final double TRANSL_Y = 158000;
    private static final double SCALE = 459;

    public record MapPoint(long x, long y) {}

    public static MapPoint savToMap(double x, double y) {
        double newX = x + TRANSL_X;
        double newY = y - TRANSL_Y;
        // X et Y volontairement inversés
        return new MapPoint(Math.round(newY / SCALE), Math.round(newX / SCALE));
    }
}
