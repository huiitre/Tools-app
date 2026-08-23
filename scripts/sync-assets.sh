#!/usr/bin/env bash
# Miroir local des assets du NAS. Le local devient la copie exacte de la source :
# ce qui a disparu du NAS est supprimé, ce qui est identique n'est pas retéléchargé.

source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

check_connection
mkdir -p "${LOCAL_ASSETS_PATH}"

log "source : ${SSH_HOST}:${REMOTE_ASSETS_PATH}"
log "cible  : ${LOCAL_ASSETS_PATH}"

rsync -a --delete --partial "${RSYNC_PROGRESS[@]}" \
    -e "ssh -p ${SSH_PORT}" \
    "${REMOTE}:${REMOTE_ASSETS_PATH}/" \
    "${LOCAL_ASSETS_PATH}/"

log "assets à jour ($(du -sh "${LOCAL_ASSETS_PATH}" | cut -f1))"
