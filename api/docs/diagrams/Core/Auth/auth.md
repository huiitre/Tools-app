# Core / Auth

> Boîte arrondie = port (interface). Trait plein = dépend de. Trait pointillé = implémente.

```mermaid
flowchart LR
  subgraph Api
  AuthController["AuthController"]
  GoogleAuthorizationUrlResponse["GoogleAuthorizationUrlResponse"]
  LoginRequest["LoginRequest"]
  LoginResponse["LoginResponse"]
  PasswordResetRequest["PasswordResetRequest"]
  PasswordResetRequestResponse["PasswordResetRequestResponse"]
  RegisterRequest["RegisterRequest"]
  RegisterResponse["RegisterResponse"]
  ResetPasswordRequest["ResetPasswordRequest"]
  SetPasswordRequest["SetPasswordRequest"]
  end
  subgraph Application
  AccessTokenData["AccessTokenData"]
  AdminSignupNotifier["AdminSignupNotifier"]
  AuthSession["AuthSession"]
  AuthSessionService["AuthSessionService"]
  CompleteGoogleOAuthLoginUseCase["CompleteGoogleOAuthLoginUseCase"]
  CreateElectronSessionUseCase["CreateElectronSessionUseCase"]
  GetGoogleAuthorizationUrlUseCase["GetGoogleAuthorizationUrlUseCase"]
  GoogleAuthenticationResult["GoogleAuthenticationResult"]
  GoogleIdentity["GoogleIdentity"]
  GoogleIdentityAuthenticationService["GoogleIdentityAuthenticationService"]
  GoogleOAuthLoginResult["GoogleOAuthLoginResult"]
  IAuthRepository(["IAuthRepository"])
  IEmailVerificationRepository(["IEmailVerificationRepository"])
  IGoogleAuthRepository(["IGoogleAuthRepository"])
  IGoogleIdentityVerifier(["IGoogleIdentityVerifier"])
  IGoogleOAuthClient(["IGoogleOAuthClient"])
  IGoogleOAuthStateStore(["IGoogleOAuthStateStore"])
  IPasswordHasher(["IPasswordHasher"])
  IPasswordResetRepository(["IPasswordResetRepository"])
  IRegistrationRepository(["IRegistrationRepository"])
  ITokenService(["ITokenService"])
  IUserAuthProviderRepository(["IUserAuthProviderRepository"])
  IUserCredentialsRepository(["IUserCredentialsRepository"])
  IssuedToken["IssuedToken"]
  LoginUseCase["LoginUseCase"]
  RefreshSessionUseCase["RefreshSessionUseCase"]
  RefreshTokenData["RefreshTokenData"]
  RegisterUserCommand["RegisterUserCommand"]
  RegisterUserUseCase["RegisterUserUseCase"]
  RegisteredAccount["RegisteredAccount"]
  RequestPasswordResetUseCase["RequestPasswordResetUseCase"]
  ResetPasswordUseCase["ResetPasswordUseCase"]
  SetUserPasswordCommand["SetUserPasswordCommand"]
  SetUserPasswordUseCase["SetUserPasswordUseCase"]
  VerifyEmailUseCase["VerifyEmailUseCase"]
  end
  subgraph Domain
  AuthUser["AuthUser"]
  end
  subgraph Infrastructure
  BCryptPasswordHasher["BCryptPasswordHasher"]
  EmailVerificationCleanupService["EmailVerificationCleanupService"]
  GoogleOAuthClient["GoogleOAuthClient"]
  GoogleOAuthOptions["GoogleOAuthOptions"]
  GoogleOAuthStateStore["GoogleOAuthStateStore"]
  GoogleOidcTokenVerifier["GoogleOidcTokenVerifier"]
  JwtAuthenticationExtensions["JwtAuthenticationExtensions"]
  JwtOptions["JwtOptions"]
  JwtTokenParameters["JwtTokenParameters"]
  JwtTokenService["JwtTokenService"]
  PasswordResetCleanupService["PasswordResetCleanupService"]
  PasswordResetOptions["PasswordResetOptions"]
  PostgresAuthRepository["PostgresAuthRepository"]
  PostgresEmailVerificationRepository["PostgresEmailVerificationRepository"]
  PostgresGoogleAuthRepository["PostgresGoogleAuthRepository"]
  PostgresPasswordResetRepository["PostgresPasswordResetRepository"]
  PostgresRegistrationRepository["PostgresRegistrationRepository"]
  PostgresUserAuthProviderRepository["PostgresUserAuthProviderRepository"]
  PostgresUserCredentialsRepository["PostgresUserCredentialsRepository"]
  RefreshTokenCookieManager["RefreshTokenCookieManager"]
  RegistrationOptions["RegistrationOptions"]
  end
  subgraph Autre
  AuthModule["AuthModule"]
  end
  AuthController --> CompleteGoogleOAuthLoginUseCase
  AuthController --> CreateElectronSessionUseCase
  AuthController --> GetGoogleAuthorizationUrlUseCase
  AuthController --> GoogleOAuthOptions
  AuthController --> LoginUseCase
  AuthController --> RefreshSessionUseCase
  AuthController --> RefreshTokenCookieManager
  AuthController --> RegisterUserUseCase
  AuthController --> RequestPasswordResetUseCase
  AuthController --> ResetPasswordUseCase
  AuthController --> SetUserPasswordUseCase
  AuthController --> VerifyEmailUseCase
  AuthSessionService --> IAuthRepository
  AuthSessionService --> ITokenService
  AuthSessionService --> AuthUser
  BCryptPasswordHasher -.-> IPasswordHasher
  CompleteGoogleOAuthLoginUseCase --> AdminSignupNotifier
  CompleteGoogleOAuthLoginUseCase --> AuthSessionService
  CompleteGoogleOAuthLoginUseCase --> GoogleIdentityAuthenticationService
  CompleteGoogleOAuthLoginUseCase --> IGoogleIdentityVerifier
  CompleteGoogleOAuthLoginUseCase --> IGoogleOAuthClient
  CompleteGoogleOAuthLoginUseCase --> IGoogleOAuthStateStore
  CreateElectronSessionUseCase --> IAuthRepository
  CreateElectronSessionUseCase --> ITokenService
  GetGoogleAuthorizationUrlUseCase --> IGoogleOAuthClient
  GetGoogleAuthorizationUrlUseCase --> IGoogleOAuthStateStore
  GoogleAuthenticationResult --> AuthUser
  GoogleIdentityAuthenticationService --> IGoogleAuthRepository
  GoogleOAuthClient --> GoogleOAuthOptions
  GoogleOAuthClient -.-> IGoogleOAuthClient
  GoogleOAuthLoginResult --> AuthSession
  GoogleOAuthStateStore -.-> IGoogleOAuthStateStore
  GoogleOidcTokenVerifier -.-> IGoogleIdentityVerifier
  IAuthRepository --> AuthUser
  IGoogleAuthRepository --> AuthUser
  ITokenService --> AuthUser
  JwtTokenService --> JwtOptions
  JwtTokenService -.-> ITokenService
  JwtTokenService --> AuthUser
  LoginUseCase --> AuthSessionService
  LoginUseCase --> IAuthRepository
  LoginUseCase --> IPasswordHasher
  PostgresAuthRepository -.-> IAuthRepository
  PostgresAuthRepository --> AuthUser
  PostgresEmailVerificationRepository -.-> IEmailVerificationRepository
  PostgresGoogleAuthRepository -.-> IGoogleAuthRepository
  PostgresGoogleAuthRepository --> AuthUser
  PostgresPasswordResetRepository -.-> IPasswordResetRepository
  PostgresRegistrationRepository -.-> IRegistrationRepository
  PostgresUserAuthProviderRepository -.-> IUserAuthProviderRepository
  PostgresUserCredentialsRepository -.-> IUserCredentialsRepository
  RefreshSessionUseCase --> AuthSessionService
  RefreshSessionUseCase --> IAuthRepository
  RefreshSessionUseCase --> ITokenService
  RefreshTokenCookieManager --> JwtOptions
  RegisterUserUseCase --> AdminSignupNotifier
  RegisterUserUseCase --> IEmailVerificationRepository
  RegisterUserUseCase --> IPasswordHasher
  RegisterUserUseCase --> IRegistrationRepository
  RegisterUserUseCase --> RegistrationOptions
  RequestPasswordResetUseCase --> IAuthRepository
  RequestPasswordResetUseCase --> IPasswordResetRepository
  RequestPasswordResetUseCase --> IUserAuthProviderRepository
  RequestPasswordResetUseCase --> PasswordResetOptions
  ResetPasswordUseCase --> IPasswordHasher
  ResetPasswordUseCase --> IPasswordResetRepository
  ResetPasswordUseCase --> IUserCredentialsRepository
  SetUserPasswordUseCase --> IAuthRepository
  SetUserPasswordUseCase --> IPasswordHasher
  SetUserPasswordUseCase --> IUserAuthProviderRepository
  SetUserPasswordUseCase --> IUserCredentialsRepository
  VerifyEmailUseCase --> AdminSignupNotifier
  VerifyEmailUseCase --> IEmailVerificationRepository
  VerifyEmailUseCase --> IRegistrationRepository
```
