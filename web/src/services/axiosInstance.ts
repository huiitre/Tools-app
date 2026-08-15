import axios, { AxiosInstance } from 'axios';
import { useAuthStore } from '@/modules/Auth/auth.store';
import { ApiException } from './ApiException';

/* ======================
   CLIENT FACTORY
====================== */

const createClient = (version: string): AxiosInstance =>
  axios.create({
    baseURL: `${import.meta.env.VITE_TOOLS_API_BASE_URL}/api/${version}`,
    headers: {
      'Content-Type': 'application/json',
    },
    withCredentials: true,
  });

//* Adresse de l'API Core.
//*
//* En QA et en production, les deux APIs sont derrière la même origine et le reverse proxy
//* route par chemin : /api/v3 vers Java, /api/core vers le Core (qui reçoit les requêtes
//* débarrassées du préfixe). C'est le cas par défaut, aucune variable à renseigner.
//*
//* En développement le Core est un process séparé, écouté sur son propre port et sans
//* préfixe : VITE_TOOLS_CORE_BASE_URL permet alors de le viser directement
//* (ex. http://localhost:5090).
const CORE_BASE_URL =
  import.meta.env.VITE_TOOLS_CORE_BASE_URL ||
  `${import.meta.env.VITE_TOOLS_API_BASE_URL}/api/core`;

//* Client sans intercepteur, réservé aux appels de session (refresh, /me, logout) :
//* un 401 dessus ne doit jamais relancer un refresh. Il vise l'API Core comme clientCore.
const clientInit = axios.create({
  baseURL: CORE_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

const clientV1 = createClient('v1');
const clientV2 = createClient('v2');
const clientV3 = createClient('v3');
const clientV3Dofus = createClient('v3');

//* API Core : identité, profil et — à terme — notifications et realtime.
//*
//* Le front ne connaît qu'une adresse : ni le langage, ni la version, ni la machine qui la
//* sert. Un changement d'implémentation côté Core ne se voit pas ici.
const clientCore = axios.create({
  baseURL: CORE_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
});

/* ======================
   SESSION
====================== */

//* Routes de session, servies par l'API Core (clientInit vise déjà /api/core).
const REFRESH_URL = '/auth/refresh';
const ME_URL = '/users/me';

//* Un seul refresh à la fois, partagé par tous les appelants.
//*
//* Sans ce partage, cinq requêtes qui reçoivent un 401 en même temps (typique au retour
//* d'onglet) déclenchent cinq POST /auth/refresh en parallèle, et autant de GET /user/me
//* derrière. Ici, la première crée la promesse, les suivantes attendent la même.
let refreshPromise: Promise<string> | null = null;

export const refreshSession = (): Promise<string> => {
  //* Un refresh est déjà en vol → on s'y raccroche au lieu d'en lancer un second.
  if (refreshPromise) return refreshPromise;

  const auth = useAuthStore();

  const promise = (async () => {
    //* clientInit n'a pas d'intercepteur : un 401 sur ces deux appels ne peut pas
    //* relancer un refresh, donc pas de récursion possible.
    const { data } = await clientInit.post(REFRESH_URL);
    auth.setToken(data.accessToken);

    //* Les droits affichés viennent de /me. Sans cet appel ils restent figés depuis le
    //* chargement de la page, alors que le token, lui, est réémis avec des rôles relus
    //* en base : l'interface et l'API finissent par ne plus dire la même chose.
    try {
      const me = await clientInit.get(ME_URL, {
        headers: { Authorization: `Bearer ${data.accessToken}` },
      });
      auth.setUser(me.data);
    } catch {
      //* La session reste valide, le token vient d'être renouvelé. On garde le profil
      //* connu plutôt que de déconnecter quelqu'un sur un /me momentanément indisponible.
    }

    return data.accessToken as string;
  })();

  refreshPromise = promise;

  //* Libère le verrou dans les deux cas. Les deux handlers sont passés au même .then
  //* pour que le rejet soit considéré comme traité ici : la promesse rendue aux
  //* appelants reste celle qu'ils doivent catcher eux-mêmes.
  const release = () => {
    if (refreshPromise === promise) refreshPromise = null;
  };
  promise.then(release, release);

  return promise;
};

/* ======================
   INTERCEPTORS
====================== */

const attachInterceptors = (client: AxiosInstance) => {
  /* ---------- REQUEST ---------- */
  client.interceptors.request.use((config) => {
    const auth = useAuthStore();

    if (auth.accessToken) {
      config.headers = config.headers || {};
      config.headers.Authorization = `Bearer ${auth.accessToken}`;
    }

    return config;
  });

  /* ---------- RESPONSE ---------- */
  client.interceptors.response.use(
    (response) => response,
    async (error) => {
      const auth = useAuthStore();
      const status = error?.response?.status;
      const originalRequest = error.config;

      /* -----------------------------
         PAS UNE 401 → ON REMONTE
      ----------------------------- */
      if (status !== 401) {
        const responseData = error.response?.data;

        if (responseData?.message) {
          return Promise.reject(
            new ApiException(responseData.message, status, responseData.code),
          );
        }

        return Promise.reject(error);
      }

      /* -----------------------------
         401 SUR /auth/login
      ----------------------------- */
      if (originalRequest.url.includes('/auth/login')) {
        const responseData = error.response?.data;

        return Promise.reject(
          new ApiException(
            responseData?.message ?? 'Identifiants invalides',
            status,
            responseData?.code,
          ),
        );
      }

      /* -----------------------------
         401 SUR /auth/refresh
         → SESSION MORTE
      ----------------------------- */
      const wasAuthenticated = !!auth.user;
      if (originalRequest.url.includes('/auth/refresh')) {
        auth.logout();

        if (wasAuthenticated) {
          window.dispatchEvent(new Event('auth:expired'));
        }

        return new Promise(() => {});
      }

      /* -----------------------------
         SÉCURITÉ : PAS DE BOUCLE
      ----------------------------- */
      if (originalRequest._retry) {
        auth.logout();
        window.dispatchEvent(new Event('auth:expired'));
        return new Promise(() => {});
      }

      /* -----------------------------
         TENTATIVE DE REFRESH
      ----------------------------- */
      originalRequest._retry = true;

      try {
        //* Refresh partagé : plusieurs 401 simultanés ne produisent qu'un seul appel.
        //* Le retry repart ensuite avec le nouveau token, posé par l'intercepteur de requête.
        await refreshSession();
        return client(originalRequest);
      } catch (refreshError) {
        auth.logout();
        window.dispatchEvent(new Event('auth:expired'));
        return new Promise(() => {});
      }
    },
  );
};

/* ======================
   INIT
====================== */

attachInterceptors(clientV1);
attachInterceptors(clientV2);
attachInterceptors(clientV3);
attachInterceptors(clientV3Dofus);
attachInterceptors(clientCore);

/* ======================
   INTERCEPTOR DOFUS
====================== */

import { useDofusStore } from '@/modules/Dofus/dofus.store';

clientV3Dofus.interceptors.request.use((config) => {
  const dofus = useDofusStore();

  if (dofus.currentGameVersionId !== null) {
    config.headers = config.headers || {};
    config.headers['X-Game-Version-Id'] = dofus.currentGameVersionId;
    config.headers['X-Game-Serve-Id'] = dofus.currentGameServerId;
  }

  return config;
});

export { clientV1, clientV2, clientV3, clientInit, clientV3Dofus, clientCore };
