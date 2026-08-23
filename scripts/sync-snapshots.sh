#!/usr/bin/env bash
# Miroir local des snapshots serveur Palworld. Même principe que les assets :
# copie exacte de la source, sans retélécharger ce qui n'a pas changé.

source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

check_connection
mkdir -p "${LOCAL_SNAPSHOTS_PATH}"

log "source : ${SSH_HOST}:${REMOTE_SNAPSHOTS_PATH}"
log "cible  : ${LOCAL_SNAPSHOTS_PATH}"

rsync -a --delete --partial "${RSYNC_PROGRESS[@]}" \
    -e "ssh -p ${SSH_PORT}" \
    "${REMOTE}:${REMOTE_SNAPSHOTS_PATH}/" \
    "${LOCAL_SNAPSHOTS_PATH}/"

log "snapshots à jour ($(du -sh "${LOCAL_SNAPSHOTS_PATH}" | cut -f1))"
