CREATE TABLE tools_dofus.almanax_subscription (
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id     BIGINT NOT NULL REFERENCES tools_core.users(id) ON DELETE CASCADE,
    almanax_id  BIGINT NOT NULL REFERENCES tools_dofus.almanax(id) ON DELETE CASCADE,
    date        DATE NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT uq_almanax_subscription UNIQUE (user_id, almanax_id)
);

CREATE INDEX idx_almanax_subscription_date ON tools_dofus.almanax_subscription(date);
CREATE INDEX idx_almanax_subscription_user_id ON tools_dofus.almanax_subscription(user_id);
