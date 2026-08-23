#!/usr/bin/env node
// Lance l'environnement de dev (API Java, API Core, Web) et sert leurs logs en temps réel
// sur une page locale (SSE) : scroll natif, filtre par process, clear, copier.
//
// Postgres tourne en local dans docker-compose.dev.yml : la console le démarre, suit ses logs
// comme n'importe quel autre process, et l'arrête en même temps que le reste.
//
// `node dev-console/server.js qa` : ne lance que le web (mode qa), pas de détection DB.
'use strict';

const fs = require('fs');
const net = require('net');
const path = require('path');
const http = require('http');
const { spawn } = require('child_process');

const ROOT = path.resolve(__dirname, '..');
// Le shell (ex. ~/.bashrc.d/*.sh) peut déjà exporter DB_NAME/DB_USERNAME/etc. — .env ne fait
// que surcharger par-dessus pour une machine qui ne les a pas déjà, jamais les masquer.
const ENV = { ...process.env, ...loadEnvFile(path.join(ROOT, '.env')) };
const QA = process.argv.includes('qa');
const LOCAL_DB_PORT = 5433;
const COMPOSE_FILE = 'docker-compose.dev.yml';
const DB_SERVICE = 'postgres';
const CONSOLE_PORT = Number(ENV.DEV_CONSOLE_PORT) || 4488;
const MAX_BUFFER = 3000;

function loadEnvFile(filePath) {
  const result = {};
  if (!fs.existsSync(filePath)) return result;
  for (const rawLine of fs.readFileSync(filePath, 'utf8').split('\n')) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) continue;
    const idx = line.indexOf('=');
    if (idx === -1) continue;
    result[line.slice(0, idx).trim()] = line.slice(idx + 1).trim();
  }
  return result;
}

function checkPort(host, port, timeoutMs = 1000) {
  return new Promise((resolve) => {
    const socket = new net.Socket();
    let done = false;
    const finish = (ok) => {
      if (done) return;
      done = true;
      socket.destroy();
      resolve(ok);
    };
    socket.setTimeout(timeoutMs);
    socket.once('connect', () => finish(true));
    socket.once('timeout', () => finish(false));
    socket.once('error', () => finish(false));
    socket.connect(port, host);
  });
}

async function waitForPort(host, port, timeoutMs, intervalMs = 500) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (await checkPort(host, port, 1000)) return true;
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return false;
}

// ---- État partagé : buffers de logs + clients SSE connectés ----

const buffers = new Map(); // name -> [{name, kind, text|status, ts}]
const lastStatus = new Map(); // name -> 'running' | 'stopped'
const clients = new Set(); // res SSE ouverts
const children = new Map(); // name -> ChildProcess
const specs = new Map(); // name -> {command, args, env} pour permettre un restart

function broadcast(entry) {
  let buf = buffers.get(entry.name);
  if (!buf) {
    buf = [];
    buffers.set(entry.name, buf);
  }
  buf.push(entry);
  if (buf.length > MAX_BUFFER) buf.shift();
  const payload = `data: ${JSON.stringify(entry)}\n\n`;
  for (const res of clients) res.write(payload);
}

function pushLine(name, text, kind = 'log') {
  broadcast({ name, kind, text, ts: Date.now() });
}

function pushStatus(name, status) {
  lastStatus.set(name, status);
  broadcast({ name, kind: 'status', status, ts: Date.now() });
}

function killChild(child, signal = 'SIGTERM') {
  if (!child) return;
  try {
    // Groupe de process entier (npm -> mvn -> dotnet watch -> ..., etc.), pas que le process
    // de tête, sinon les petits-enfants restent orphelins (vécu plusieurs fois aujourd'hui).
    process.kill(-child.pid, signal);
  } catch (e) {
    // process déjà mort
  }
}

// Filet de sécurité au-delà du group-kill : `dotnet watch` a été observé survivant à un
// SIGTERM/SIGKILL de groupe (probablement un setsid interne qui l'en fait sortir) — on
// reconstruit l'arbre réel via /proc et on tue chaque descendant individuellement.
function killTree(rootPid, signal) {
  let pids;
  try {
    pids = fs.readdirSync('/proc').filter((n) => /^\d+$/.test(n)).map(Number);
  } catch (e) {
    return;
  }
  const childrenByPpid = new Map();
  for (const pid of pids) {
    let ppid;
    try {
      const stat = fs.readFileSync(`/proc/${pid}/stat`, 'utf8');
      const afterComm = stat.slice(stat.lastIndexOf(')') + 2).split(' ');
      ppid = Number(afterComm[1]);
    } catch (e) {
      continue;
    }
    if (!childrenByPpid.has(ppid)) childrenByPpid.set(ppid, []);
    childrenByPpid.get(ppid).push(pid);
  }
  const toKill = [];
  const stack = [rootPid];
  while (stack.length) {
    const pid = stack.pop();
    toKill.push(pid);
    stack.push(...(childrenByPpid.get(pid) || []));
  }
  for (let i = toKill.length - 1; i >= 0; i--) {
    try {
      process.kill(toKill[i], signal);
    } catch (e) {
      // déjà mort
    }
  }
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

function composeArgs(...args) {
  return ['compose', '-f', COMPOSE_FILE, ...args];
}

// Commande courte dont on veut la sortie dans un panneau, et dont on attend la fin — par
// opposition à spawnLogged, réservé aux process longs qu'on peut redémarrer.
function runLogged(name, command, args, envOverrides = {}) {
  return new Promise((resolve) => {
    pushLine(name, `$ ${command} ${args.join(' ')}`, 'system');
    const child = spawn(command, args, { cwd: ROOT, env: { ...process.env, ...envOverrides } });
    const onData = (chunk) => {
      chunk
        .toString('utf8')
        .split(/\r?\n/)
        .forEach((line) => {
          if (line.length) pushLine(name, line, 'log');
        });
    };
    child.stdout.on('data', onData);
    child.stderr.on('data', onData);
    child.on('exit', (code) => resolve(code === 0));
    child.on('error', (err) => {
      pushLine(name, `--- échec du lancement : ${err.message} ---`, 'system');
      resolve(false);
    });
  });
}

function spawnLogged(name, command, args, envOverrides = {}) {
  specs.set(name, { command, args, envOverrides });
  pushLine(name, `$ ${command} ${args.join(' ')}`, 'system');

  const child = spawn(command, args, {
    cwd: ROOT,
    env: { ...process.env, ...envOverrides },
    detached: true, // process group séparé, cf. killChild
  });
  children.set(name, child);
  pushStatus(name, 'running');

  const onData = (chunk) => {
    chunk
      .toString('utf8')
      .split(/\r?\n/)
      .forEach((line) => {
        if (line.length) pushLine(name, line, 'log');
      });
  };
  child.stdout.on('data', onData);
  child.stderr.on('data', onData);

  child.on('exit', (code, signal) => {
    if (children.get(name) === child) children.delete(name);
    pushLine(name, `--- process terminé (code=${code} signal=${signal}) ---`, 'system');
    pushStatus(name, 'stopped');
  });
  child.on('error', (err) => {
    pushLine(name, `--- échec du lancement : ${err.message} ---`, 'system');
    pushStatus(name, 'stopped');
  });

  return child;
}

function restart(name) {
  const spec = specs.get(name);
  if (!spec) return false;
  restartAsync(name, spec).catch((err) => pushLine(name, `--- erreur au redémarrage : ${err.message} ---`, 'system'));
  return true;
}

async function restartAsync(name, spec) {
  const existing = children.get(name);
  if (existing) {
    killChild(existing, 'SIGTERM');
    await sleep(1200);
    // Toujours là après le SIGTERM de groupe ? Filet dur avant de relancer, sinon le nouveau
    // process peut se prendre un conflit de port avec l'ancien encore agonisant.
    killChild(existing, 'SIGKILL');
    killTree(existing.pid, 'SIGKILL');
    await sleep(300);
  }
  pushLine(name, '--- redémarrage manuel ---', 'system');

  // Le process du panneau db n'est qu'un `docker compose logs -f` : le relancer seul ne
  // redémarrerait rien. C'est le conteneur que le bouton doit redémarrer.
  if (name === 'db') {
    await runLogged('db', 'docker', composeArgs('restart', DB_SERVICE), spec.envOverrides);
    await waitForPort('127.0.0.1', LOCAL_DB_PORT, 30000);
  }

  spawnLogged(name, spec.command, spec.args, spec.envOverrides);
}

// Coupe un process sans le relancer : libère son port pour un lancement externe (typiquement
// l'API démarrée depuis VS Code avec des breakpoints). Le spec est conservé, donc le bouton
// de redémarrage du même panneau sait le relancer.
function stop(name) {
  if (!specs.has(name)) return false;
  stopAsync(name).catch((err) => pushLine(name, `--- erreur à l'arrêt : ${err.message} ---`, 'system'));
  return true;
}

async function stopAsync(name) {
  pushLine(name, '--- arrêt manuel ---', 'system');
  const existing = children.get(name);
  if (existing) {
    killChild(existing, 'SIGTERM');
    await sleep(1200);
    killChild(existing, 'SIGKILL');
    killTree(existing.pid, 'SIGKILL');
  }

  // Le process du panneau db n'est qu'un `docker compose logs -f` : couper le suivi des logs
  // ne coupe pas la base. Le volume, lui, est conservé.
  if (name === 'db') {
    await runLogged('db', 'docker', [...composeArgs('stop', DB_SERVICE), '-t', '5'], specs.get(name).envOverrides);
  }

  pushStatus(name, 'stopped');
}

// Les 3 process applicatifs (ou juste web en mode qa). La base en est exclue : la redémarrer
// couperait les connexions des API alors que « tout relancer » vise le code, pas l'infra.
function restartAll() {
  const names = QA ? ['web'] : ['api', 'java', 'web'];
  for (const name of names) restart(name);
}

// ---- Orchestration : DB puis les 3 process (ou juste web en mode qa) ----

async function main() {
  if (QA) {
    spawnLogged('web', 'npm', ['run', 'web:dev:qa']);
    return;
  }

  // api (C#) ET java (Spring) lisent tous les deux DB_HOST/DB_PORT/DB_NAME/DB_USERNAME/
  // DB_PASSWORD (cf. PostgresConnectionString.cs côté api, application-dev.properties côté
  // java) : un seul et même override pour les deux, pas la peine de les traiter séparément.
  // DB_NAME/USERNAME/PASSWORD ne sont PAS touchés ici : soit déjà exportés globalement (setup
  // existant de ce poste), soit à renseigner dans .env pour une machine qui ne les a pas.
  const credentialsFromEnv = {};
  if (ENV.DB_NAME) credentialsFromEnv.DB_NAME = ENV.DB_NAME;
  if (ENV.DB_USERNAME) credentialsFromEnv.DB_USERNAME = ENV.DB_USERNAME;
  if (ENV.DB_PASSWORD) credentialsFromEnv.DB_PASSWORD = ENV.DB_PASSWORD;

  await runLogged('db', 'docker', composeArgs('up', '-d'), credentialsFromEnv);

  // Le conteneur peut répondre « démarré » avant que Postgres n'accepte les connexions : les API
  // partiraient alors sur une base injoignable. On attend le port avant de les lancer.
  if (!(await waitForPort('127.0.0.1', LOCAL_DB_PORT, 30000))) {
    pushLine(
      'db',
      `Postgres toujours injoignable sur 127.0.0.1:${LOCAL_DB_PORT} après 30s — les API vont échouer à se connecter.`,
      'system'
    );
  }

  // Le panneau db suit les logs du conteneur. Son bouton ⟳ redémarre le conteneur (cf. restartAsync).
  spawnLogged('db', 'docker', composeArgs('logs', '-f', '--tail', '50', DB_SERVICE), credentialsFromEnv);

  const dbEnv = { DB_HOST: '127.0.0.1', DB_PORT: String(LOCAL_DB_PORT), ...credentialsFromEnv };

  // Un enfant Node n'hérite pas de ~/.bashrc : sans ça, `mvn` peut retomber sur le Java par
  // défaut de SDKMAN (peut différer de la version utilisée dans un terminal interactif).
  const sourceSdkman = '[ -s "$HOME/.sdkman/bin/sdkman-init.sh" ] && source "$HOME/.sdkman/bin/sdkman-init.sh"; ';
  spawnLogged('java', 'bash', ['-c', sourceSdkman + 'exec npm run java:dev'], dbEnv);
  spawnLogged('api', 'npm', ['run', 'api:dev'], dbEnv);
  spawnLogged('web', 'npm', ['run', 'web:dev']);
}

// ---- Serveur HTTP : page + flux SSE + restart/stop ----

const indexHtml = fs.readFileSync(path.join(__dirname, 'public', 'index.html'));

const server = http.createServer((req, res) => {
  const url = new URL(req.url, `http://localhost:${CONSOLE_PORT}`);

  if (url.pathname === '/') {
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(indexHtml);
    return;
  }

  if (url.pathname === '/events') {
    res.writeHead(200, {
      'Content-Type': 'text/event-stream; charset=utf-8',
      'Cache-Control': 'no-cache',
      Connection: 'keep-alive',
    });
    res.write(':ok\n\n');
    for (const buf of buffers.values()) {
      for (const entry of buf) res.write(`data: ${JSON.stringify(entry)}\n\n`);
    }
    for (const [name, status] of lastStatus) {
      res.write(`data: ${JSON.stringify({ name, kind: 'status', status, ts: Date.now() })}\n\n`);
    }
    clients.add(res);
    req.on('close', () => clients.delete(res));
    return;
  }

  if (url.pathname === '/restart-all' && req.method === 'POST') {
    restartAll();
    res.writeHead(200, { 'Content-Type': 'text/plain' });
    res.end('ok');
    return;
  }

  const restartMatch = url.pathname.match(/^\/restart\/([a-z]+)$/);
  if (restartMatch && req.method === 'POST') {
    const ok = restart(restartMatch[1]);
    res.writeHead(ok ? 200 : 404, { 'Content-Type': 'text/plain' });
    res.end(ok ? 'ok' : 'unknown process');
    return;
  }

  const stopMatch = url.pathname.match(/^\/stop\/([a-z]+)$/);
  if (stopMatch && req.method === 'POST') {
    const ok = stop(stopMatch[1]);
    res.writeHead(ok ? 200 : 404, { 'Content-Type': 'text/plain' });
    res.end(ok ? 'ok' : 'unknown process');
    return;
  }

  res.writeHead(404, { 'Content-Type': 'text/plain' });
  res.end('not found');
});

server.listen(CONSOLE_PORT, () => {
  console.log(`dev-console: http://localhost:${CONSOLE_PORT}`);
  main().catch((err) => {
    console.error(err);
    process.exitCode = 1;
  });
});

let shuttingDown = false;
function shutdown() {
  if (shuttingDown) return;
  shuttingDown = true;
  const toKill = Array.from(children.values());
  for (const child of toKill) killChild(child, 'SIGTERM');

  // Le conteneur Postgres n'est pas un enfant de ce process : sans ça, il survivrait au Ctrl+C
  // et resterait debout sans que personne ne l'ait demandé. Le volume, lui, est conservé.
  if (!QA) {
    try {
      require('child_process').spawnSync('docker', [...composeArgs('stop'), '-t', '5'], {
        cwd: ROOT,
        stdio: 'ignore',
      });
    } catch (e) {
      // docker absent ou déjà arrêté
    }
  }
  // Filet de sécurité : dotnet watch (et parfois npm) laissent un petit-fils hors du groupe
  // de temps en temps — SIGKILL le groupe entier si toujours là après 1.5s.
  setTimeout(() => {
    for (const child of toKill) {
      killChild(child, 'SIGKILL');
      killTree(child.pid, 'SIGKILL');
    }
    process.exit(0);
  }, 1500);
}
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
