#!/usr/bin/env bash
# Applique sur la base locale le dump lu sur l'entrée standard. Pendant du db-restore.sh du NAS,
# avec la même garantie : la cible n'est détruite qu'après une restauration complète dans une base
# temporaire, elle-même supprimée quoi qu'il arrive.
#
#   npm run db:clone -- prod
#   bash scripts/db-restore.sh < .local/db/prod.dump     (rejouer un dump déjà téléchargé)
#
# Ce script ne s'adresse qu'au conteneur local. Il ne se connecte jamais au NAS.

source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

: "${DB_NAME:?DB_NAME manquant — exporte-le dans ton shell ou renseigne-le dans .env}"
: "${DB_USERNAME:?DB_USERNAME manquant}"

CONTAINER="${PG_CONTAINER:-tools_postgres_dev}"

if ! docker inspect "${CONTAINER}" >/dev/null 2>&1; then
    echo "Conteneur ${CONTAINER} introuvable. Lance d'abord : npm run db:up" >&2
    exit 1
fi

if [[ -t 0 ]]; then
    echo "Aucun dump sur l'entrée standard." >&2
    echo "Usage : npm run db:clone -- prod   ou   bash $0 < fichier.dump" >&2
    exit 1
fi

sql() {
    docker exec "${CONTAINER}" psql -U "${DB_USERNAME}" -d postgres -v ON_ERROR_STOP=1 -tAc "$1" </dev/null
}

TEMP_DB="${DB_NAME}_restore_$$"

cleanup() {
    sql "DROP DATABASE IF EXISTS \"${TEMP_DB}\" WITH (FORCE);" >/dev/null 2>&1 || true
}
trap cleanup EXIT

log "restauration dans la base temporaire ${TEMP_DB}"
sql "CREATE DATABASE \"${TEMP_DB}\" OWNER \"${DB_USERNAME}\";" >/dev/null
docker exec -i "${CONTAINER}" pg_restore -U "${DB_USERNAME}" -d "${TEMP_DB}" \
    --no-owner --no-privileges --exit-on-error

log "bascule ${TEMP_DB} → ${DB_NAME}"
sql "DROP DATABASE IF EXISTS \"${DB_NAME}\" WITH (FORCE);" >/dev/null
sql "ALTER DATABASE \"${TEMP_DB}\" RENAME TO \"${DB_NAME}\";" >/dev/null

log "base locale ${DB_NAME} restaurée"
