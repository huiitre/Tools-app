# Core / Mail

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  InternalMailController["InternalMailController"]
  MailController["MailController"]
  SendMailAttachmentRequest["SendMailAttachmentRequest"]
  SendMailRequest["SendMailRequest"]
  end
  subgraph Application
  IMailSender(["IMailSender"])
  MailAttachment["MailAttachment"]
  MailService["MailService"]
  SendMailCommand["SendMailCommand"]
  SendMailUseCase["SendMailUseCase"]
  end
  subgraph Infrastructure
  SmtpMailOptions["SmtpMailOptions"]
  SmtpMailSender["SmtpMailSender"]
  end
  subgraph Autre
  MailModule["MailModule"]
  end
  InternalMailController --> SendMailUseCase
  MailController --> SendMailUseCase
  MailService --> IMailSender
  SendMailUseCase --> MailService
  SmtpMailSender --> SmtpMailOptions
  SmtpMailSender -.-> IMailSender
```
