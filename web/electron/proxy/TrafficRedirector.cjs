/**
 * Service de redirection réseau (Désactivé)
 * L'application utilise maintenant une approche de proxy manuel ou via arguments de lancement.
 */
class TrafficRedirector {
    async enable() {
        // Ne fait plus rien - Pas de modification d'iptables
        return Promise.resolve();
    }

    async disable() {
        // Ne fait plus rien
        return Promise.resolve();
    }
}

module.exports = new TrafficRedirector();
