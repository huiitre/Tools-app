const http = require('http')
const logger = require('../logger/LoggerService.cjs')

// Serveur HTTP local qui reçoit le token à la fin de la connexion Google.
// Il remplace le lien tools:// : après les redirections de Google, le navigateur refusait
// parfois de l'ouvrir, sans rien afficher (constaté le 27/11/2026).
//
// - port choisi par le système (0) : jamais de conflit avec un autre serveur local ;
// - 127.0.0.1 uniquement : invisible depuis le réseau ;
// - ouvert le temps d'une connexion : fermé au token reçu, à la connexion suivante ou au délai.

const TIMEOUT_MS = 5 * 60 * 1000

const SUCCESS_PAGE = `<!doctype html>
<html lang="fr">
<head><meta charset="utf-8"><title>Tools</title>
<style>
  body { margin: 0; height: 100vh; display: grid; place-items: center;
         font-family: system-ui, sans-serif; background: #13171f; color: #e0e3e7; }
  main { text-align: center; }
  p { color: #9aa3ad; }
</style></head>
<body><main><h1>Connexion réussie</h1><p>Vous pouvez fermer cet onglet et revenir sur Tools.</p></main></body>
</html>`

let current = null

function stop() {
  if (!current) return
  clearTimeout(current.timer)
  current.server.close()
  current = null
}

// Démarre l'écoute et résout le port attribué. onToken est appelé une seule fois.
function start(onToken) {
  stop()

  return new Promise((resolve, reject) => {
    const server = http.createServer((req, res) => {
      const url = new URL(req.url, 'http://127.0.0.1')
      const token = url.searchParams.get('token')

      if (req.method !== 'GET' || url.pathname !== '/auth/callback' || !token) {
        res.writeHead(404).end()
        return
      }

      res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' }).end(SUCCESS_PAGE)
      stop()
      onToken(token)
    })

    server.once('error', (err) => {
      logger.error('GoogleLoopback', `Écoute impossible : ${err.message}`)
      reject(err)
    })

    server.listen(0, '127.0.0.1', () => {
      const { port } = server.address()
      const timer = setTimeout(() => {
        logger.warn('GoogleLoopback', 'Aucun retour de Google, écoute arrêtée')
        stop()
      }, TIMEOUT_MS)
      current = { server, timer }
      logger.info('GoogleLoopback', `En attente du retour de Google sur le port ${port}`)
      resolve(port)
    })
  })
}

module.exports = { start, stop }
