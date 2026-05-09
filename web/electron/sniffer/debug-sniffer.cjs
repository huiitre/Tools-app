const snifferService = require('./SnifferService.cjs');

console.log("=== DEBUG SNIFFER START ===");
console.log("Assure-toi d'être connecté en jeu et de cliquer sur un item.");

try {
    snifferService.start((data) => {
        console.log("\n[!] DONNÉES REÇUES :");
        console.table(data); // Affiche un beau tableau dans le terminal
    });
} catch (error) {
    console.error("ERREUR AU DÉMARRAGE :", error.message);
}

// Pour arrêter proprement avec Ctrl+C
process.on('SIGINT', () => {
    console.log("\nArrêt du sniffer...");
    snifferService.stop();
    process.exit();
});