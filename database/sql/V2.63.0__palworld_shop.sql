-- =========================================================================
-- Module Palworld — Marchands (Shop)
-- Réutilise le catalogue item existant (jusqu'ici alimenté uniquement par
-- les drops de Pals) plutôt que de créer une table d'items parallèle : les
-- deux sources partagent le même identifiant brut (StaticItemId du jeu).
-- =========================================================================

ALTER TABLE tools_palworld.item
    ADD COLUMN price INTEGER,
    ADD COLUMN max_stack_count INTEGER;

COMMENT ON COLUMN tools_palworld.item.price IS 'Prix de référence (hors contexte marchand), alimenté par la sync shop';
COMMENT ON COLUMN tools_palworld.item.max_stack_count IS 'Taille de stack max, alimenté par la sync shop';

CREATE TABLE tools_palworld.merchant (
    id                BIGSERIAL PRIMARY KEY,
    external_id       VARCHAR(100) NOT NULL UNIQUE,
    code              VARCHAR(100) NOT NULL,
    name              VARCHAR(255),
    portrait_url      TEXT,
    restock_minute    INTEGER,
    currency_item_id  VARCHAR(150) NOT NULL
);

COMMENT ON TABLE  tools_palworld.merchant                   IS 'Marchands PNJ (source: merchants.json), 22/25 sans nom individuel réel dans le jeu';
COMMENT ON COLUMN tools_palworld.merchant.external_id       IS 'Identifiant stable côté extracteur (ex: "arena_shop", "male_trader_v04")';
COMMENT ON COLUMN tools_palworld.merchant.code               IS 'Nom court du blueprint source (info technique, pas un nom affichable)';
COMMENT ON COLUMN tools_palworld.merchant.name               IS 'NULL pour les vendeurs génériques sans nom individuel dans le jeu (pas une jointure manquante)';
COMMENT ON COLUMN tools_palworld.merchant.restock_minute     IS 'Délai de réapprovisionnement en minutes, NULL si inconnu (ex: bounty_trader)';
COMMENT ON COLUMN tools_palworld.merchant.currency_item_id   IS 'StaticItemId de la devise (ex: "Money", "BattleTicket") — pas toujours présent dans tools_palworld.item, pas de FK';

CREATE TABLE tools_palworld.merchant_offer (
    id                      BIGSERIAL PRIMARY KEY,
    merchant_id             BIGINT NOT NULL REFERENCES tools_palworld.merchant(id) ON DELETE CASCADE,
    item_id                 BIGINT NOT NULL REFERENCES tools_palworld.item(id),
    price                   INTEGER NOT NULL,
    quantity_per_purchase   INTEGER NOT NULL,
    product_type            VARCHAR(30) NOT NULL CHECK (product_type IN ('NORMAL', 'ONLY_PURCHASE_ONE')),
    UNIQUE (merchant_id, item_id)
);

COMMENT ON TABLE  tools_palworld.merchant_offer                     IS 'Offres d''un marchand (source: merchants.json[].offers), prix déjà résolu par l''extracteur';
COMMENT ON COLUMN tools_palworld.merchant_offer.price               IS 'Prix réellement facturé par CE marchand, peut différer du item.price de référence';
COMMENT ON COLUMN tools_palworld.merchant_offer.product_type        IS 'NORMAL = rachetable à chaque réappro, ONLY_PURCHASE_ONE = achetable une seule fois';

CREATE INDEX idx_merchant_offer_merchant ON tools_palworld.merchant_offer(merchant_id);
CREATE INDEX idx_merchant_offer_item     ON tools_palworld.merchant_offer(item_id);
