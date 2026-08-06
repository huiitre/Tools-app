CREATE INDEX IF NOT EXISTS idx_pal_instance_present_last_seen
    ON tools_palworld.pal_instance (is_present, last_seen_at);

CREATE INDEX IF NOT EXISTS idx_pal_instance_present_pal_instance
    ON tools_palworld.pal_instance (is_present, pal_id, instance_id);
