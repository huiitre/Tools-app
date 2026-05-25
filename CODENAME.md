Codename sur Tools – Spécifications Complètes
Vue d'ensemble
Implémentation d'un jeu Codename multijoueur intégré à l'écosystème Tools, avec authentification hybride (invités + comptes Tools), persistance SQL, WebSocket temps réel, et système d'historique.

Architecture Technique
Stack

Backend : Java 21, Spring Boot, DDD strict
Frontend : Vue.js 3, PicoCSS
Communication : WebSocket existant de Tools (STOMP)
Base de données : PostgreSQL (schéma tools_codename)
Déploiement : Docker, même infra que Tools

Couches (DDD)
domain/          -- GameSession, Card, Player (0 dépendance)
application/     -- Use cases (CreateGameUseCase, JoinGameUseCase...)
infrastructure/  -- Repositories JPA, WebSocket config
api/             -- Controllers REST + WebSocket
config/          -- Security, CORS, DB

Modèle de Données SQL
Tables principales
1. codename_words
sqlCREATE TABLE codename_words (
    id UUID PRIMARY KEY,
    content VARCHAR(50) NOT NULL UNIQUE,
    validated BOOLEAN DEFAULT TRUE
);
2. codename_tags
sqlCREATE TABLE codename_tags (
    id UUID PRIMARY KEY,
    label VARCHAR(30) NOT NULL UNIQUE
);
3. codename_words_tags (M:N)
sqlCREATE TABLE codename_words_tags (
    word_id UUID REFERENCES codename_words(id) ON DELETE CASCADE,
    tag_id UUID REFERENCES codename_tags(id) ON DELETE CASCADE,
    PRIMARY KEY (word_id, tag_id)
);
4. codename_word_proposals
sqlCREATE TABLE codename_word_proposals (
    id UUID PRIMARY KEY,
    content VARCHAR(50) NOT NULL,
    suggested_tags JSONB,
    proposed_by UUID REFERENCES tools_core.users(id),
    status VARCHAR(20), -- 'PENDING', 'VALIDATED', 'REJECTED'
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
5. codename_sessions
sqlCREATE TABLE codename_sessions (
    id UUID PRIMARY KEY,
    owner_id UUID REFERENCES tools_core.users(id), -- Nullable (peut être créé par un invité qui se connecte après)
    status VARCHAR(20), -- 'LOBBY', 'IN_PROGRESS', 'FINISHED'
    state_json JSONB, -- Board complet (25 cartes avec couleurs), scores, tour actuel
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
Structure du state_json :
json{
  "board": [
    { "word": "Chat", "color": "RED", "revealed": false },
    { "word": "Souris", "color": "BLUE", "revealed": true },
    ...
  ],
  "currentTurn": "RED",
  "scores": { "RED": 9, "BLUE": 8 },
  "startingTeam": "RED",
  "clue": { "word": "Animal", "count": 2 }
}
6. codename_session_players
sqlCREATE TABLE codename_session_players (
    id UUID PRIMARY KEY, -- Le player_session_id du localStorage
    session_id UUID REFERENCES codename_sessions(id) ON DELETE CASCADE,
    user_id UUID REFERENCES tools_core.users(id), -- Nullable
    nickname VARCHAR(50) NOT NULL,
    team VARCHAR(10), -- 'RED', 'BLUE', NULL (spectateur)
    role VARCHAR(20), -- 'SPYMASTER', 'OPERATIVE', 'SPECTATOR'
    is_ready BOOLEAN DEFAULT FALSE,
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
7. codename_events
sqlCREATE TABLE codename_events (
    id UUID PRIMARY KEY,
    session_id UUID REFERENCES codename_sessions(id) ON DELETE CASCADE,
    player_session_id UUID,
    type VARCHAR(20), -- 'CHAT_MSG', 'CARD_CLICK', 'CLUE_GIVEN', 'PLAYER_JOIN', 'PLAYER_READY', 'TEAM_CHANGE', 'ROLE_CHANGE', 'GAME_START', 'GAME_END'
    content TEXT, -- Message chat ou données JSON
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
8. codename_history
sqlCREATE TABLE codename_history (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES tools_core.users(id),
    session_id UUID REFERENCES codename_sessions(id),
    team VARCHAR(10),
    role VARCHAR(20),
    result VARCHAR(10), -- 'WIN', 'LOSS'
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

Fonctionnalités par Module
Module Admin (JWT obligatoire)
Route : /api/v3/codename/admin
Permissions :

Lecture : Tous les utilisateurs connectés
Écriture : Rôle CODENAME_MODERATOR (table tools_core.user_roles à créer si inexistante)

Endpoints :

Créer une session

POST /api/v3/codename/admin/sessions
Body : { "tags": ["Anime", "Expert"] } (optionnel)
Génère UUID → Insert codename_sessions avec status = LOBBY
Retourne : { "gameUrl": "tools.huiitre.fr/codename/game/{uuid}" }


CRUD Mots

GET /api/v3/codename/admin/words?tag=Anime&page=0&size=50
POST /api/v3/codename/admin/words → Body : { "content": "Naruto", "tagIds": [...] }
PATCH /api/v3/codename/admin/words/{id} → Modifier tags
DELETE /api/v3/codename/admin/words/{id}


CRUD Tags

GET /api/v3/codename/admin/tags
POST /api/v3/codename/admin/tags → { "label": "Manga" }
DELETE /api/v3/codename/admin/tags/{id} (cascade sur codename_words_tags)


Gestion Propositions

GET /api/v3/codename/admin/proposals?status=PENDING
PATCH /api/v3/codename/admin/proposals/{id}/validate → Crée le mot + tags
PATCH /api/v3/codename/admin/proposals/{id}/reject


Proposer un mot (accessible à tous)

POST /api/v3/codename/proposals
Body : { "content": "Pikachu", "suggestedTags": ["Jeux Vidéo"] }
Insert dans codename_word_proposals avec status = PENDING




Module Jeu (Public)
Routes REST

Rejoindre une session

POST /api/v3/codename/game/{sessionId}/join
Body : { "playerSessionId": "uuid-localStorage", "nickname": "Huiitre" }
Headers (optionnel) : Authorization: Bearer {jwt}
Logic :

Vérifie si player_session_id existe déjà → reconnexion
Sinon insert dans codename_session_players avec role = SPECTATOR
Si JWT présent, lie user_id


Retourne : État complet de la session


Récupérer l'état

GET /api/v3/codename/game/{sessionId}
Retourne :

codename_sessions.state_json
Liste des joueurs (codename_session_players)
Si status = FINISHED : Retourne aussi codename_events pour replay




Historique personnel

GET /api/v3/codename/history (JWT requis)
Retourne : Liste paginée depuis codename_history WHERE user_id = :current_user


Historique public

GET /api/v3/codename/history/{userId} (optionnel, à implémenter plus tard)



WebSocket (STOMP)
Topics :

/topic/game/{sessionId} → Broadcast à tous les participants

Destinations (client → serveur) :

/app/codename/{sessionId}/select-team → Body : { "team": "RED" }
/app/codename/{sessionId}/select-role → Body : { "role": "SPYMASTER" }
/app/codename/{sessionId}/ready → Toggle is_ready
/app/codename/{sessionId}/chat → Body : { "message": "Salut" }
/app/codename/{sessionId}/give-clue → Body : { "word": "Animal", "count": 2 }
/app/codename/{sessionId}/click-card → Body : { "cardIndex": 12 }
/app/codename/{sessionId}/restart → Demande de rejeu

Messages serveur → client (broadcast) :
json{
  "type": "STATE_UPDATE",
  "payload": {
    "players": [...],
    "board": [...], // Spectateurs ne voient pas les couleurs non révélées
    "currentTurn": "RED",
    "scores": { "RED": 9, "BLUE": 8 }
  }
}

Logique Métier (Domain)
Aggregate : GameSession
Entities :

GameId (Value Object, UUID)
Board (Entity, 25x Card)
Card (VO : word, color, revealed)
PlayerList (Collection de Player)
Player (VO : playerId, nickname, team, role, isReady)

Invariants :

Minimum 4 joueurs pour démarrer (2 RED, 2 BLUE, au moins 1 spymaster par équipe)
Tous doivent être isReady = true
Une carte ne peut être cliquée que par l'équipe au tour actuel
Seul l'OPERATIVE peut cliquer sur les cartes
Seul le SPYMASTER peut donner un indice

Méthodes Domain :
javapublic class GameSession {
    public void selectTeam(PlayerId playerId, Team team);
    public void selectRole(PlayerId playerId, Role role);
    public void toggleReady(PlayerId playerId);
    public void start(); // Génère la grille si tous prêts
    public void giveClue(PlayerId playerId, String word, int count);
    public void clickCard(PlayerId playerId, int cardIndex);
    public boolean isFinished();
    public Team getWinner();
    public void restart(); // Reset board, garde les équipes
}
Génération de la grille
Algorithme :

Sélectionner 25 mots aléatoires depuis codename_words (avec filtrage tags si spécifié à la création)
Tirer au sort l'équipe qui commence (RED ou BLUE)
Distribution des couleurs :

9 cartes pour l'équipe qui commence
8 cartes pour l'équipe adverse
7 cartes neutres
1 carte assassin


Mélanger l'ordre des cartes

Important : Les spectateurs ne voient JAMAIS les couleurs non révélées. Le state_json en base contient tout, mais le WebSocket envoie une version filtrée selon le rôle.

Workflow Complet
1. Création de partie (Admin Tools)

User connecté va sur /codename/admin
Clique "Créer une partie"
(Optionnel) Sélectionne des tags
Backend :

Insert codename_sessions avec status = LOBBY
Retourne URL


User copie l'URL et la partage

2. Arrivée sur l'URL

Client charge /codename/game/{uuid}
Frontend vérifie localStorage.getItem('codename_player_id')

Si absent → Génère UUID, stocke


Affiche modal :

Input pseudo (si pas de JWT)
Bouton "Se connecter avec Tools"


User saisit pseudo OU se connecte
Frontend appelle POST /game/{uuid}/join avec playerSessionId + nickname (+ JWT optionnel)
Backend insert dans codename_session_players avec role = SPECTATOR
Frontend connecte le WebSocket au topic /topic/game/{uuid}
User arrive dans la vue "Lobby"

3. Phase Lobby

Affichage :

Liste des joueurs avec leur équipe/rôle
Boutons "Rejoindre RED" / "Rejoindre BLUE"
Boutons "Devenir SPYMASTER" / "Devenir OPERATIVE"
Bouton "Prêt" (désactivé si spectateur)


Actions :

Clic sur équipe → WebSocket /app/codename/{uuid}/select-team
Backend :

Update codename_session_players.team
Reset is_ready = false
Broadcast STATE_UPDATE




Validation démarrage :

Backend vérifie en continu si conditions remplies
Si oui → Auto-start :

Génération de la grille
Update status = IN_PROGRESS
Insert codename_events (type GAME_START)
Broadcast STATE_UPDATE avec le board





4. Phase Jeu

Affichage :

Grille 5x5
Spectateurs : Cartes blanches (ou grises si révélées)
Joueurs : Cartes blanches
Spymasters : Vision complète des couleurs
Chat latéral avec events intégrés


Tour RED :

Spymaster RED donne un indice → WebSocket /give-clue
Backend :

Vérifie que c'est bien le spymaster de l'équipe au tour
Update state_json.clue
Insert codename_events (type CLUE_GIVEN)
Broadcast


Operative RED clique sur une carte → WebSocket /click-card
Backend :

Vérifie que c'est bien un operative de l'équipe au tour
Révèle la carte (revealed = true)
Insert codename_events (type CARD_CLICK)
Logic :

Si carte RED → L'équipe peut continuer
Si carte BLUE → Tour passe à BLUE
Si carte neutre → Tour passe à BLUE
Si carte assassin → BLUE gagne immédiatement


Broadcast




Fin de partie :

Détection : Toutes les cartes d'une équipe révélées OU assassin révélé
Backend :

Update status = FINISHED
Insert codename_events (type GAME_END)
Pour chaque joueur avec user_id non NULL :

Insert codename_history


Broadcast STATE_UPDATE avec winner




Rejouer :

Tous cliquent sur "Rejouer" → WebSocket /restart
Backend :

Compte les votes
Si unanimité :

Génère nouvelle grille
Reset is_ready = false pour tous
Update status = LOBBY
Broadcast







5. Replay (Partie terminée)

User arrive sur /codename/game/{uuid} d'une partie FINISHED
Frontend détecte le statut → Ne connecte PAS le WebSocket
Affiche :

Grille finale (toutes les cartes révélées)
Chat avec timeline des events
Bouton "Voir le replay étape par étape" (optionnel, phase 2)




Frontend Vue.js
Routes
javascript{
  path: '/codename',
  children: [
    { path: 'admin', component: CodenameAdmin, meta: { requiresAuth: true } },
    { path: 'game/:sessionId', component: CodenameGame },
    { path: 'history', component: CodenameHistory, meta: { requiresAuth: true } }
  ]
}
Composants principaux
CodenameAdmin.vue

Liste des mots (pagination, filtres tags)
CRUD mots/tags
Liste des propositions (badge count si PENDING)
Bouton "Créer une partie"

CodenameGame.vue

Sous-composants :

LobbyView.vue → Sélection équipe/rôle, liste joueurs, bouton prêt
BoardView.vue → Grille 5x5, affichage conditionnel selon rôle
ChatView.vue → Messages + events (types différenciés visuellement)
ReplayView.vue → Timeline des coups (si FINISHED)



CodenameHistory.vue

Liste paginée des parties jouées
Filtres : équipe, rôle, résultat
Clic sur une ligne → Redirect vers /codename/game/{uuid} (replay)

Gestion du WebSocket
javascript// store/codename.js
import { Client } from '@stomp/stompjs';

const stompClient = new Client({
  brokerURL: 'wss://tools.huiitre.fr/ws-codename',
  connectHeaders: {
    Authorization: `Bearer ${getAccessToken()}` // Si JWT présent
  },
  onConnect: () => {
    stompClient.subscribe(`/topic/game/${sessionId}`, (message) => {
      const payload = JSON.parse(message.body);
      // Dispatch Vuex action
    });
  }
});
Gestion du localStorage
javascript// utils/playerSession.js
export function getOrCreatePlayerId() {
  let playerId = localStorage.getItem('codename_player_id');
  if (!playerId) {
    playerId = crypto.randomUUID();
    localStorage.setItem('codename_player_id', playerId);
  }
  return playerId;
}

Backend Java 21
Structure des packages
com/huiitre/tools/codename/
├── domain/
│   ├── model/
│   │   ├── GameSession.java
│   │   ├── Card.java
│   │   ├── Player.java
│   │   ├── Team.java (enum)
│   │   ├── Role.java (enum)
│   │   └── GameStatus.java (enum)
│   ├── service/
│   │   ├── GameService.java
│   │   └── BoardGenerator.java
│   └── repository/ (interfaces)
│       ├── GameSessionRepository.java
│       └── WordRepository.java
│
├── application/
│   ├── usecase/
│   │   ├── CreateGameUseCase.java
│   │   ├── JoinGameUseCase.java
│   │   ├── SelectTeamUseCase.java
│   │   ├── GiveClueUseCase.java
│   │   └── ClickCardUseCase.java
│   └── dto/
│       ├── GameStateDTO.java
│       └── PlayerDTO.java
│
├── infrastructure/
│   ├── persistence/
│   │   ├── JpaGameSessionRepository.java
│   │   ├── JpaWordRepository.java
│   │   └── entities/ (JPA)
│   │       ├── GameSessionEntity.java
│   │       ├── WordEntity.java
│   │       └── ...
│   └── websocket/
│       ├── WebSocketConfig.java
│       └── StompChannelInterceptor.java
│
└── api/
    ├── rest/
    │   ├── CodenameAdminController.java
    │   └── CodenameGameController.java
    └── websocket/
        └── GameWebSocketController.java
Exemple Use Case
java@Service
@Transactional
public class ClickCardUseCase {
    
    private final GameSessionRepository repository;
    private final SimpMessagingTemplate messagingTemplate;
    
    public void execute(UUID sessionId, UUID playerId, int cardIndex) {
        GameSession session = repository.findById(sessionId)
            .orElseThrow(() -> new SessionNotFoundException(sessionId));
        
        session.clickCard(playerId, cardIndex); // Domain logic
        
        repository.save(session);
        
        // Event logging
        eventRepository.save(new GameEvent(sessionId, playerId, "CARD_CLICK", cardIndex));
        
        // WebSocket broadcast
        GameStateDTO state = buildStateForPlayers(session);
        messagingTemplate.convertAndSend(
            "/topic/game/" + sessionId,
            new StateUpdateMessage(state)
        );
        
        if (session.isFinished()) {
            archiveGame(session);
        }
    }
    
    private GameStateDTO buildStateForPlayers(GameSession session) {
        // Filtre les couleurs selon le rôle (spectateurs ne voient pas)
    }
}

Tâches de Développement
Phase 1 : Infrastructure

Migration SQL (Liquibase/Flyway) pour les 8 tables
Entities JPA
Repositories (JPA + interfaces domain)
Configuration WebSocket (réutiliser l'existant, ajouter interceptor pour JWT optionnel)

Phase 2 : Domain

Value Objects (Team, Role, GameStatus, Card)
Aggregate GameSession avec invariants
Service BoardGenerator (logique de génération grille)
Tests unitaires Domain (isolation totale)

Phase 3 : Application

Use Cases (Create, Join, SelectTeam, SelectRole, ToggleReady, Start, GiveClue, ClickCard, Restart)
DTOs (GameStateDTO avec filtre spectateur)
Mappers Domain ↔ DTO

Phase 4 : API

CodenameAdminController (CRUD mots/tags, propositions)
CodenameGameController (REST : join, get state, history)
GameWebSocketController (@MessageMapping pour les actions en jeu)
Sécurité : Intercepteur WebSocket pour JWT optionnel

Phase 5 : Frontend

Routes Vue Router
Store Vuex/Pinia (gestion état WebSocket)
Composants Lobby/Board/Chat/Replay
Intégration PicoCSS (variables pour thème RED/BLUE)
Gestion localStorage (player_session_id)

Phase 6 : Polissage

Scheduled task (purge parties > 48h)
Tests E2E (Playwright : créer partie, rejoindre, jouer)
Logs (SLF4J + Logback)
Documentation API (Swagger/OpenAPI)


Points d'Attention
Sécurité

Validation stricte côté serveur (ne jamais faire confiance au client)
Vérifier que le joueur a le droit de faire l'action (tour, rôle)
Sanitize les messages de chat (XSS)

Performance

Cache en mémoire (Caffeine) pour codename_sessions actives
Index DB sur session_id, user_id, created_at
Pagination sur historique (max 50 parties par page)

UX

Reconnexion WebSocket silencieuse si déconnexion temporaire
Feedback visuel immédiat (optimistic UI) avant confirmation serveur
Animations CSS pour révélation de cartes
Son optionnel (toggle dans settings)

Edge Cases

Que faire si un spymaster quitte ? → Promouvoir un operative ou bloquer la partie
Gestion des doublons de pseudos dans une même session → Ajouter suffixe (#1, #2)
Spam de messages → Rate limiting côté WebSocket


Livrables Attendus

Code Backend : Respectant DDD strict, tests unitaires Domain
Code Frontend : Vue.js 3 avec composables, PicoCSS
Migration SQL : Liquibase avec rollback
Documentation : README avec schéma architecture, guide déploiement
Tests : Coverage > 80% sur Domain, E2E sur scénarios critiques