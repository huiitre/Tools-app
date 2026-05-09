package fr.huiitre.tools.modules.dofus.almanax.domain;

import java.time.LocalDate;
import java.util.Objects;

public class DatePattern {

    private final String raw;
    private final String day;
    private final String month;
    private final String year;

    public DatePattern(String raw) {
        if (raw == null || raw.isBlank()) {
            throw new IllegalArgumentException("ALMANAX_PATTERN_REQUIRED");
        }

        String[] parts = raw.split("/");
        if (parts.length != 3) {
            throw new IllegalArgumentException("ALMANAX_PATTERN_INVALID");
        }

        this.raw = raw;
        this.day = parts[0];
        this.month = parts[1];
        this.year = parts[2];
    }

    /** Est-ce que ce pattern correspond à ce jour */
    public boolean matches(LocalDate date) {
        return matchesPart(day, date.getDayOfMonth())
                && matchesPart(month, date.getMonthValue())
                && matchesPart(year, date.getYear());
    }

    /**
     * Priorité métier :
     * 3 = DD/MM/YYYY
     * 2 = DD/MM/*
     * 1 = * /MM/ *
     * 0 = no match
     */
    public int score(LocalDate date) {
        if (!matches(date))
            return 0;

        if (!"*".equals(year))
            return 3;
        if (!"*".equals(day))
            return 2;
        return 1;
    }

    private boolean matchesPart(String pattern, int value) {
        if ("*".equals(pattern))
            return true;
        return Integer.parseInt(pattern) == value;
    }

    public String raw() {
        return raw;
    }

    @Override
    public boolean equals(Object o) {
        return (o instanceof DatePattern p) && Objects.equals(raw, p.raw);
    }

    @Override
    public int hashCode() {
        return Objects.hash(raw);
    }

    @Override
    public String toString() {
        return raw;
    }
}
