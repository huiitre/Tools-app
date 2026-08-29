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
  SendInternalMailUseCase["SendInternalMailUseCase"]
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
  InternalMailController --> SendInternalMailUseCase
  MailController --> SendMailUseCase
  MailService --> IMailSender
  SendInternalMailUseCase --> MailService
  SendMailUseCase --> MailService
  SmtpMailSender --> SmtpMailOptions
  SmtpMailSender -.-> IMailSender
```
