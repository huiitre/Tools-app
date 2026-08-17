# Mail

Pour envoyer un email depuis un autre use case, injecter `MailService` puis appeler `Send` :

```csharp
await mailService.Send(
    new SendMailCommand(
        ["user@example.com"],
        "Réinitialisation de votre mot de passe",
        Text: "Voici votre lien : ..."),
    cancellationToken);
```

Les pièces jointes sont facultatives :

```csharp
new MailAttachment("rapport.txt", "text/plain", fileContent)
```

`MailService` contient la validation et dépend du port `IMailSender`. L’implémentation actuelle est SMTP (`SmtpMailSender`). Si le serveur doit un jour utiliser Sendmail, il suffit d’ajouter `SendmailMailSender : IMailSender` et de modifier une ligne d’enregistrement dans `Program.cs`.

## Endpoint pour l’API Java

```text
POST /mail
```

```json
{
  "to": ["user@example.com"],
  "subject": "Rapport Dofus",
  "text": "Le rapport est en pièce jointe.",
  "attachments": [
    {
      "fileName": "report.txt",
      "contentType": "text/plain",
      "contentBase64": "Y29udGVudSBkdSByYXBwb3J0"
    }
  ]
}
```

Les pièces jointes sont toujours envoyées en Base64 : aucun appelant ne transmet donc jamais un chemin de fichier qui ne serait pas accessible depuis le conteneur Core.

`SendMailUseCase` exige le rôle `ADMIN` ou supérieur. Le contrôle est porté par le use case, pas par la route : voir `docs/SECURITY.md`.

Les use cases internes du Core n’utilisent jamais ce use case : ils injectent `MailService` directement et ne passent donc par aucun contrôle de rôle.

## Endpoint de service à service

```text
POST /internal/mail
```

Même contrat JSON que `POST /mail` ci-dessus. Réservé aux appelants machine — l'API Java pour
commencer, le temps de retirer progressivement sa propre gestion de l'envoi d'email — jamais à
un utilisateur : la route n'accepte aucun jeton JWT, uniquement le secret partagé attendu par
`[InternalApi]` (voir `docs/ARCHITECTURE.md`, section réservée à `/internal/`), dans l'en-tête

```text
X-Internal-Token: <INTERNAL_API_TOKEN>
```

Un appel sans le secret, ou avec un secret incorrect, renvoie `404` — jamais `401`, pour ne pas
confirmer l'existence de la route à qui la cherche. Même modèle que `POST /internal/notifications`.

`InternalMailController` appelle `SendInternalMailUseCase`, pas `MailService` directement : un
endpoint appelle toujours un use case, qui orchestre et appelle le service — l'absence
d'utilisateur courant à autoriser ne change rien à cette règle, elle explique seulement pourquoi
ce use case n'est pas un `SecuredUseCase` (qui exige un `CurrentUser` résolu depuis une requête
authentifiée) mais une classe autonome, sans rôle ni module requis.

Les requêtes Bruno correspondantes sont dans `bruno/Tools API Core/Mail/`.

## Configuration SMTP

Les valeurs non sensibles sont versionnées : `Port` et `EnableSsl` dans `appsettings.json`,
`Host` et `FromAddress` dans `appsettings.QA.json` et `appsettings.Production.json`.

Seuls les identifiants viennent de l'environnement, **sous les mêmes noms que l'API Java** :

```yaml
MAIL_USERNAME: "admin@huiitre.fr"
MAIL_PASSWORD: "..."
```

Le compte SMTP est le même pour les deux APIs ; leur donner deux noms de variable pour une
seule valeur ferait perdre du temps à l'exploitation. C'est le choix déjà fait pour
`JWT_SECRET`, `DB_HOST` et `GOOGLE_CLIENT_ID`.

`SmtpMailSender` lit la variable en priorité et retombe sur la section `Mail:Smtp` :

```csharp
private readonly string? username = environment["MAIL_USERNAME"] ?? options.Value.Username;
```

En local, `api-core/appsettings.Local.json` — non versionné, chargé en option par
`Program.cs` — continue donc de fonctionner sans variable d'environnement.

Si l'un des quatre champs `Host`, `Username`, `Password` ou `FromAddress` manque, tout envoi
échoue avec `503 MAIL_NOT_CONFIGURED`. Les deux flux concernés sont l'inscription et la
demande de réinitialisation de mot de passe.
