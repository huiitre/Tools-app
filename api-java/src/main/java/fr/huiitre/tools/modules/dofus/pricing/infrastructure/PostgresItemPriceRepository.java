package fr.huiitre.tools.modules.dofus.pricing.infrastructure;

import java.sql.Timestamp;
import java.util.List;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

import fr.huiitre.tools.modules.dofus.pricing.application.ports.ItemPriceRepository;
import fr.huiitre.tools.modules.dofus.pricing.application.view.ItemPriceDto;

public class PostgresItemPriceRepository implements ItemPriceRepository {

    private final NamedParameterJdbcTemplate jdbcTemplate;
    private static final Logger logger = LoggerFactory.getLogger(PostgresItemPriceRepository.class);

    public PostgresItemPriceRepository(NamedParameterJdbcTemplate jdbcTemplate) {
        this.jdbcTemplate = jdbcTemplate;
    }

    private static final RowMapper<ItemPriceDto> ITEM_PRICE_DTO_ROW_MAPPER =
        (rs, rowNum) -> {

            java.sql.Array sqlArray = rs.getArray("parent_item_ids");
            Long[] parentItemIds = null;

            if (sqlArray != null) {
                parentItemIds = (Long[]) sqlArray.getArray();
            }

            return new ItemPriceDto(
                rs.getLong("item_id"),
                parentItemIds,

                rs.getLong("user_price"),
                rs.getLong("community_average_price"),
                rs.getLong("last_updated_price"),

                rs.getLong("craft_user_price"),
                rs.getLong("craft_community_price"),
                rs.getLong("craft_last_price"),
                rs.getLong("craft_calculated_price"),

                Optional.ofNullable(rs.getTimestamp("user_price_created_at"))
                    .map(Timestamp::toLocalDateTime).orElse(null),

                Optional.ofNullable(rs.getTimestamp("community_average_price_created_at"))
                    .map(Timestamp::toLocalDateTime).orElse(null),

                Optional.ofNullable(rs.getTimestamp("last_updated_price_created_at"))
                    .map(Timestamp::toLocalDateTime).orElse(null)
            );
        };

    @Override
    public List<ItemPriceDto> findPricesByItemIds(
        List<Long> itemIds,
        Long userId,
        Long serverId
    ) {

        String sql = """
            SELECT
                i.id AS item_id,
                parent.parent_item_ids,

                /* =========================
                PRIX DIRECTS
                ========================= */

                COALESCE(up.price, 0)::bigint AS user_price,
                COALESCE(cp.avg_price, 0)::bigint AS community_average_price,
                COALESCE(lp.price, 0)::bigint AS last_updated_price,

                /* =========================
                CRAFT — USER
                (craft enfant si existe, sinon PU)
                ========================= */

                CASE WHEN has_recipe THEN COALESCE(cu.price, 0)::bigint ELSE 0 END AS craft_user_price,

                /* =========================
                CRAFT — COMMU
                ========================= */

                CASE WHEN has_recipe THEN COALESCE(cc.price, 0)::bigint ELSE 0 END AS craft_community_price,

                /* =========================
                CRAFT — DERNIER
                ========================= */

                CASE WHEN has_recipe THEN COALESCE(cl.price, 0)::bigint ELSE 0 END AS craft_last_price,

                /* =========================
                CRAFT — BEST
                (PU ou craft, peu importe la source)
                ========================= */

                CASE WHEN has_recipe THEN COALESCE(cbest.price, 0)::bigint ELSE 0 END AS craft_calculated_price,

                /* =========================
                CREATED AT
                ========================= */

                up.created_at AS user_price_created_at,
                cp.created_at AS community_average_price_created_at,
                lp.created_at AS last_updated_price_created_at

            FROM tools_dofus.item i

            /* =========================
            PARENTS
            ========================= */

            LEFT JOIN LATERAL (
                SELECT ARRAY_AGG(r.item_id) AS parent_item_ids
                FROM tools_dofus.recipe r
                WHERE r.ingredient_id = i.id
            ) parent ON TRUE

            /* =========================
            HAS RECIPE
            ========================= */

            LEFT JOIN LATERAL (
                SELECT EXISTS (
                    SELECT 1
                    FROM tools_dofus.recipe r
                    WHERE r.item_id = i.id
                ) AS has_recipe
            ) rflag ON TRUE

            /* =========================
            PU USER
            ========================= */

            LEFT JOIN LATERAL (
                SELECT price, created_at
                FROM tools_dofus.item_price_user
                WHERE item_id = i.id
                AND user_id = :userId
                AND game_server_id = :serverId
                ORDER BY created_at DESC
                LIMIT 1
            ) up ON TRUE

            /* =========================
            PU COMMU
            ========================= */

            LEFT JOIN LATERAL (
                SELECT AVG(NULLIF(price, 0)) AS avg_price, MAX(created_at) AS created_at
                FROM tools_dofus.item_price_user
                WHERE item_id = i.id
                AND game_server_id = :serverId
            ) cp ON TRUE

            /* =========================
            PU LAST
            ========================= */

            LEFT JOIN LATERAL (
                SELECT price, created_at
                FROM tools_dofus.item_price_user
                WHERE item_id = i.id
                AND game_server_id = :serverId
                ORDER BY created_at DESC, id DESC
                LIMIT 1
            ) lp ON TRUE

            /* =========================
            CRAFT USER (PU ou craft enfant)
            ========================= */

            LEFT JOIN LATERAL (
                SELECT SUM(
                    r.quantity * COALESCE(
                        lvl2.price,
                        lvl1.price,
                        pu.price,
                        0
                    )
                ) AS price
                FROM tools_dofus.recipe r

                /* craft niveau 1 */
                LEFT JOIN LATERAL (
                    SELECT SUM(rr.quantity * p.price) AS price
                    FROM tools_dofus.recipe rr
                    LEFT JOIN LATERAL (
                        SELECT price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr.ingredient_id
                        AND user_id = :userId
                        AND game_server_id = :serverId
                        ORDER BY created_at DESC
                        LIMIT 1
                    ) p ON TRUE
                    WHERE rr.item_id = r.ingredient_id
                ) lvl1 ON TRUE

                /* craft niveau 2 */
                LEFT JOIN LATERAL (
                    SELECT SUM(rr2.quantity * p2.price) AS price
                    FROM tools_dofus.recipe rr1
                    JOIN tools_dofus.recipe rr2 ON rr2.item_id = rr1.ingredient_id
                    LEFT JOIN LATERAL (
                        SELECT price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr2.ingredient_id
                        AND user_id = :userId
                        AND game_server_id = :serverId
                        ORDER BY created_at DESC
                        LIMIT 1
                    ) p2 ON TRUE
                    WHERE rr1.item_id = r.ingredient_id
                ) lvl2 ON TRUE

                /* PU fallback */
                LEFT JOIN LATERAL (
                    SELECT price
                    FROM tools_dofus.item_price_user
                    WHERE item_id = r.ingredient_id
                    AND user_id = :userId
                    AND game_server_id = :serverId
                    ORDER BY created_at DESC
                    LIMIT 1
                ) pu ON TRUE

                WHERE r.item_id = i.id
            ) cu ON TRUE

            /* =========================
            CRAFT COMMU
            ========================= */

            LEFT JOIN LATERAL (
                SELECT SUM(
                    r.quantity * COALESCE(
                        lvl2.price,
                        lvl1.price,
                        pu.avg_price,
                        0
                    )
                ) AS price
                FROM tools_dofus.recipe r

                LEFT JOIN LATERAL (
                    SELECT SUM(rr.quantity * p.avg_price) AS price
                    FROM tools_dofus.recipe rr
                    LEFT JOIN LATERAL (
                        SELECT AVG(NULLIF(price, 0)) AS avg_price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr.ingredient_id
                        AND game_server_id = :serverId
                    ) p ON TRUE
                    WHERE rr.item_id = r.ingredient_id
                ) lvl1 ON TRUE

                LEFT JOIN LATERAL (
                    SELECT SUM(rr2.quantity * p2.avg_price) AS price
                    FROM tools_dofus.recipe rr1
                    JOIN tools_dofus.recipe rr2 ON rr2.item_id = rr1.ingredient_id
                    LEFT JOIN LATERAL (
                        SELECT AVG(NULLIF(price, 0)) AS avg_price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr2.ingredient_id
                        AND game_server_id = :serverId
                    ) p2 ON TRUE
                    WHERE rr1.item_id = r.ingredient_id
                ) lvl2 ON TRUE

                LEFT JOIN LATERAL (
                    SELECT AVG(NULLIF(price, 0)) AS avg_price
                    FROM tools_dofus.item_price_user
                    WHERE item_id = r.ingredient_id
                    AND game_server_id = :serverId
                ) pu ON TRUE

                WHERE r.item_id = i.id
            ) cc ON TRUE

            /* =========================
            CRAFT LAST
            ========================= */

            LEFT JOIN LATERAL (
                SELECT SUM(
                    r.quantity * COALESCE(
                        lvl2.price,
                        lvl1.price,
                        pu.price,
                        0
                    )
                ) AS price
                FROM tools_dofus.recipe r

                LEFT JOIN LATERAL (
                    SELECT SUM(rr.quantity * p.price) AS price
                    FROM tools_dofus.recipe rr
                    LEFT JOIN LATERAL (
                        SELECT price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr.ingredient_id
                        AND game_server_id = :serverId
                        ORDER BY created_at DESC, id DESC
                        LIMIT 1
                    ) p ON TRUE
                    WHERE rr.item_id = r.ingredient_id
                ) lvl1 ON TRUE

                LEFT JOIN LATERAL (
                    SELECT SUM(rr2.quantity * p2.price) AS price
                    FROM tools_dofus.recipe rr1
                    JOIN tools_dofus.recipe rr2 ON rr2.item_id = rr1.ingredient_id
                    LEFT JOIN LATERAL (
                        SELECT price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr2.ingredient_id
                        AND game_server_id = :serverId
                        ORDER BY created_at DESC, id DESC
                        LIMIT 1
                    ) p2 ON TRUE
                    WHERE rr1.item_id = r.ingredient_id
                ) lvl2 ON TRUE

                LEFT JOIN LATERAL (
                    SELECT price
                    FROM tools_dofus.item_price_user
                    WHERE item_id = r.ingredient_id
                    AND game_server_id = :serverId
                    ORDER BY created_at DESC, id DESC
                    LIMIT 1
                ) pu ON TRUE

                WHERE r.item_id = i.id
            ) cl ON TRUE

            /* =========================
            CRAFT BEST (PU OU CRAFT, PEU IMPORTE)
            ========================= */

            LEFT JOIN LATERAL (
                SELECT SUM(
                    r.quantity * COALESCE(
                        lvl2.price,
                        lvl1.price,
                        pu.price,
                        0
                    )
                ) AS price
                FROM tools_dofus.recipe r

                LEFT JOIN LATERAL (
                    SELECT SUM(rr.quantity * p.price) AS price
                    FROM tools_dofus.recipe rr
                    LEFT JOIN LATERAL (
                        SELECT price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr.ingredient_id
                        AND game_server_id = :serverId
                        ORDER BY created_at DESC, id DESC
                        LIMIT 1
                    ) p ON TRUE
                    WHERE rr.item_id = r.ingredient_id
                ) lvl1 ON TRUE

                LEFT JOIN LATERAL (
                    SELECT SUM(rr2.quantity * p2.price) AS price
                    FROM tools_dofus.recipe rr1
                    JOIN tools_dofus.recipe rr2 ON rr2.item_id = rr1.ingredient_id
                    LEFT JOIN LATERAL (
                        SELECT price
                        FROM tools_dofus.item_price_user
                        WHERE item_id = rr2.ingredient_id
                        AND game_server_id = :serverId
                        ORDER BY created_at DESC, id DESC
                        LIMIT 1
                    ) p2 ON TRUE
                    WHERE rr1.item_id = r.ingredient_id
                ) lvl2 ON TRUE

                LEFT JOIN LATERAL (
                    SELECT price
                    FROM tools_dofus.item_price_user
                    WHERE item_id = r.ingredient_id
                    AND game_server_id = :serverId
                    ORDER BY created_at DESC, id DESC
                    LIMIT 1
                ) pu ON TRUE

                WHERE r.item_id = i.id
            ) cbest ON TRUE

            WHERE i.id = ANY(:itemIds)
        """;

        MapSqlParameterSource params = new MapSqlParameterSource()
            .addValue("itemIds", itemIds.toArray(new Long[0]))
            .addValue("userId", userId)
            .addValue("serverId", serverId);

        return jdbcTemplate.query(sql, params, ITEM_PRICE_DTO_ROW_MAPPER);
    }

    @Override
    public void updateItemPrice(Long itemId, Long serverId, Long userId, Long price) {
        String sql = """
            INSERT INTO tools_dofus.item_price_user
                (item_id, game_server_id, user_id, price, created_at)
            VALUES
                (:itemId, :serverId, :userId, :price, NOW())
            ON CONFLICT (item_id, game_server_id, user_id)
            DO UPDATE SET
                price = EXCLUDED.price,
                created_at = NOW()
        """;

        MapSqlParameterSource params = new MapSqlParameterSource()
            .addValue("itemId", itemId)
            .addValue("serverId", serverId)
            .addValue("userId", userId)
            .addValue("price", price);

        jdbcTemplate.update(sql, params);
    }
}
