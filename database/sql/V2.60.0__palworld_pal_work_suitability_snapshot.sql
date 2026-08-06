ALTER TABLE tools_palworld.pal_instance
    ADD COLUMN base_work_suitability JSONB NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN work_suitability_add_ranks JSONB NOT NULL DEFAULT '{}'::jsonb;
