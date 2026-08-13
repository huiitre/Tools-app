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

La route n’a pour l’instant aucune restriction d’accès. Le blocage se fera au niveau du use case, comme côté Java, et reste à implémenter.

Les use cases internes du Core n’utilisent jamais cette route : ils injectent `MailService` directement.

Les requêtes Bruno correspondantes sont dans `bruno/Tools API Core/Mail/`.

## Configuration SMTP

En local, la section `Mail:Smtp` est renseignée dans `api-core/appsettings.Local.json`,
qui n'est pas versionné et que `Program.cs` charge en option. En conteneur, ce sont
les variables d'environnement :

```text
Mail__Smtp__Host=ssl0.ovh.net
Mail__Smtp__Port=587
Mail__Smtp__Username=...
Mail__Smtp__Password=...
Mail__Smtp__EnableSsl=true
Mail__Smtp__FromAddress=admin@huiitre.fr
```
