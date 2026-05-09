INSERT INTO tools_dofus.item_price_user (
    item_id,
    game_server_id,
    user_id,
    price
)
SELECT
    di.id AS item_id,
    (SELECT id FROM tools_dofus.game_server WHERE code = 'salar') AS game_server_id,
    u.id AS user_id,
    dui.average_price AS price
FROM tools_dofus_unity.item dui
JOIN tools_dofus.item di
    ON di.asset_id = dui.iditem
CROSS JOIN tools_core.users u
WHERE dui.average_price IS NOT NULL
ON CONFLICT (item_id, game_server_id, user_id) DO NOTHING;