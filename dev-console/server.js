#!/usr/bin/env node
// Lance l'environnement de dev (API Java, API Core, Web) et sert leurs logs en temps réel
// sur une page locale (SSE) : scroll natif, filtre par process, clear, copier.
//
// Résout l'accès Postgres dans cet ordre : tunnel local déjà ouvert -> réutilisé ; sinon
// LAN_DB_HOST (.env) joignable en direct -> pas de tunnel ; sinon tunnel SSH (identifiants
// dans .env, voir .env.example).
//
// `node dev-console/server.js qa` : ne lance que le web (mode qa), pas de détection DB.
'use strict';

const fs = require('fs');
const net = require('net');
const path = require('path');
const http = require('http');
const { spawn } = require('child_process');

const ROOT = path.resolve(__dirname, '..');
// Le shell (ex. ~/.bashrc.d/*.sh) peut déjà exporter SSH_HOST/LAN_DB_HOST/etc. — .env ne fait
// que surcharger par-dessus pour une machine qui ne les a pas déjà, jamais les masquer.
const ENV = { ...process.env, ...loadEnvFile(path.join(ROOT, '.env')) };
const QA = process.argv.includes('qa');
const LOCAL_DB_PORT = 5433;
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
  spawnLogged(name, spec.command, spec.args, spec.envOverrides);
}

// Les 3 process de dev (ou juste web en mode qa) — pas le tunnel, qui n'en fait pas partie.
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
  let dbEnv = {};
  const credentialsFromEnv = {};
  if (ENV.DB_NAME) credentialsFromEnv.DB_NAME = ENV.DB_NAME;
  if (ENV.DB_USERNAME) credentialsFromEnv.DB_USERNAME = ENV.DB_USERNAME;
  if (ENV.DB_PASSWORD) credentialsFromEnv.DB_PASSWORD = ENV.DB_PASSWORD;

  if (await checkPort('127.0.0.1', LOCAL_DB_PORT)) {
    pushLine('system', `port ${LOCAL_DB_PORT} déjà ouvert (tunnel existant réutilisé).`, 'system');
    dbEnv = { DB_HOST: '127.0.0.1', DB_PORT: String(LOCAL_DB_PORT), ...credentialsFromEnv };
  } else if (ENV.LAN_DB_HOST && (await checkPort(ENV.LAN_DB_HOST, Number(ENV.LAN_DB_PORT || 5432)))) {
    const host = ENV.LAN_DB_HOST;
    const port = ENV.LAN_DB_PORT || '5432';
    pushLine('system', `Postgres joignable en direct sur ${host} — pas de tunnel.`, 'system');
    dbEnv = { DB_HOST: host, DB_PORT: String(port), ...credentialsFromEnv };
  } else {
    const { SSH_HOST, SSH_USER, SSH_PORT } = ENV;
    if (!SSH_HOST || !SSH_USER || !SSH_PORT) {
      pushLine(
        'system',
        'Postgres injoignable et SSH_HOST/SSH_USER/SSH_PORT absents de .env (copie .env.example vers .env). Abandon.',
        'system'
      );
      process.exitCode = 1;
      return;
    }
    pushLine('system', `Postgres injoignable en direct — tunnel SSH vers ${SSH_HOST}.`, 'system');
    spawnLogged('tunnel', 'ssh', [
      '-N',
      '-o',
      'ExitOnForwardFailure=yes',
      '-L',
      `${LOCAL_DB_PORT}:127.0.0.1:5432`,
      '-p',
      SSH_PORT,
      `${SSH_USER}@${SSH_HOST}`,
    ]);
    const ok = await waitForPort('127.0.0.1', LOCAL_DB_PORT, 15000);
    if (!ok) {
      pushLine('system', 'tunnel toujours pas up après 15s, lancement quand même — voir le panneau tunnel.', 'system');
    }
    dbEnv = { DB_HOST: '127.0.0.1', DB_PORT: String(LOCAL_DB_PORT), ...credentialsFromEnv };
  }

  // Un enfant Node n'hérite pas de ~/.bashrc : sans ça, `mvn` peut retomber sur le Java par
  // défaut de SDKMAN (peut différer de la version utilisée dans un terminal interactif).
  const sourceSdkman = '[ -s "$HOME/.sdkman/bin/sdkman-init.sh" ] && source "$HOME/.sdkman/bin/sdkman-init.sh"; ';
  spawnLogged('java', 'bash', ['-c', sourceSdkman + 'exec npm run java:dev'], dbEnv);
  spawnLogged('api', 'npm', ['run', 'api:dev'], dbEnv);
  spawnLogged('web', 'npm', ['run', 'web:dev']);
}

// ---- Serveur HTTP : page + flux SSE + restart ----

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
