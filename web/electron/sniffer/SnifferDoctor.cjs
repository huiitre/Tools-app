const { execSync } = require('child_process');

function checkRequirements() {
    const results = {
        tcpdumpInstalled: false,
        tcpdumpPermissions: false,
        tsharkInstalled: false,
        tsharkPermissions: false,
        error: null
    };

    try {
        // 1. Vérifier tcpdump
        try {
            execSync('which tcpdump');
            results.tcpdumpInstalled = true;
            const capOutput = execSync('getcap $(which tcpdump)').toString();
            if (capOutput.includes('cap_net_admin') && capOutput.includes('cap_net_raw')) {
                results.tcpdumpPermissions = true;
            }
        } catch (e) {
            results.tcpdumpInstalled = false;
        }

        // 2. Vérifier tshark
        try {
            execSync('which tshark');
            results.tsharkInstalled = true;
            
            // tshark utilise dumpcap pour la capture
            const dumpcapPath = execSync('which dumpcap').toString().trim();
            const capOutput = execSync(`getcap ${dumpcapPath}`).toString();
            if (capOutput.includes('cap_net_admin') && capOutput.includes('cap_net_raw')) {
                results.tsharkPermissions = true;
            }
        } catch (e) {
            results.tsharkInstalled = false;
        }

        // Pour la compatibilité ascendante si besoin
        results.hasPermissions = results.tcpdumpPermissions || results.tsharkPermissions;
    } catch (err) {
        results.error = err.message;
    }

    return results;
}

module.exports = { checkRequirements };