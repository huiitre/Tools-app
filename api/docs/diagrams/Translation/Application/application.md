# Translation / Application

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Application
  DetectLanguageUseCase["DetectLanguageUseCase"]
  DetectedLanguage["DetectedLanguage"]
  ITranslationProvider(["ITranslationProvider"])
  ListTranslationLanguagesUseCase["ListTranslationLanguagesUseCase"]
  ProviderTranslation["ProviderTranslation"]
  TranslateCommand["TranslateCommand"]
  TranslateUseCase["TranslateUseCase"]
  TranslationLanguage["TranslationLanguage"]
  TranslationService["TranslationService"]
  TranslationView["TranslationView"]
  end
  DetectLanguageUseCase --> TranslationService
  ListTranslationLanguagesUseCase --> TranslationService
  TranslateUseCase --> TranslationService
  TranslationService --> ITranslationProvider
```
