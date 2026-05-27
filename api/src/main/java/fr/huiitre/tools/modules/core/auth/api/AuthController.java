package fr.huiitre.tools.modules.core.auth.api;

import fr.huiitre.tools.modules.core.auth.api.dto.GoogleLoginRequest;
import fr.huiitre.tools.modules.core.auth.api.dto.LoginRequest;
import fr.huiitre.tools.modules.core.auth.api.dto.LoginResponse;
import fr.huiitre.tools.modules.core.auth.api.dto.RegisterRequest;
import fr.huiitre.tools.modules.core.auth.api.dto.RegisterResponse;
import fr.huiitre.tools.modules.core.auth.application.command.LoginUserCommand;
import fr.huiitre.tools.modules.core.auth.application.command.RegisterUserCommand;
import fr.huiitre.tools.modules.core.auth.application.exception.UserNotFoundException;
import fr.huiitre.tools.modules.core.auth.application.usecase.AuthenticateUserWithProviderUseCase;
import fr.huiitre.tools.modules.core.auth.application.usecase.LoginUserUseCase;
import fr.huiitre.tools.modules.core.auth.application.usecase.RegisterUserAndSendVerificationUseCase;
import fr.huiitre.tools.modules.core.auth.application.usecase.RequestPasswordResetUseCase;
import fr.huiitre.tools.modules.core.auth.application.usecase.ValidateEmailVerificationUseCase;
import fr.huiitre.tools.modules.core.auth.application.usecase.ValidatePasswordResetUseCase;
import fr.huiitre.tools.modules.core.auth.domain.AuthProvider;
import fr.huiitre.tools.modules.core.auth.infrastructure.google.GoogleOAuthClient;
import fr.huiitre.tools.modules.core.auth.infrastructure.google.GoogleOAuthStateService;
import fr.huiitre.tools.modules.core.auth.infrastructure.google.GoogleTokenVerifier;
import fr.huiitre.tools.modules.core.auth.infrastructure.google.GoogleUserPayload;
import fr.huiitre.tools.modules.core.security.infrastructure.JwtProvider;
import fr.huiitre.tools.modules.core.security.infrastructure.RefreshTokenCookieManager;
import fr.huiitre.tools.modules.core.user.application.ports.UserRepository;
import fr.huiitre.tools.modules.core.user.domain.User;
import io.jsonwebtoken.Claims;
import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.validation.Valid;
import java.io.IOException;
import java.util.Date;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@Tag(name = "Core - Auth")
@RestController
@RequestMapping("/auth")
public class AuthController {

	private static final Logger logger = LoggerFactory.getLogger(AuthController.class);

	private final JwtProvider jwtProvider;
	private final LoginUserUseCase loginUserUseCase;
	private final UserRepository userRepository;
	private final AuthenticateUserWithProviderUseCase authenticateUserWithProviderUseCase;
	private final GoogleTokenVerifier googleTokenVerifier;
	private final GoogleOAuthClient googleOAuthClient;
	private final GoogleOAuthStateService googleOAuthStateService;
	private final ValidateEmailVerificationUseCase validateEmailVerificationUseCase;
	private final RegisterUserAndSendVerificationUseCase registerUserAndSendVerificationUseCase;
	private final RequestPasswordResetUseCase requestPasswordResetUseCase;
	private final ValidatePasswordResetUseCase validatePasswordResetUseCase;
	private final RefreshTokenCookieManager refreshTokenCookieManager;

	@Value("${app.frontend.base-url}")
	private String frontendBaseUrl;

	public AuthController(
			JwtProvider jwtProvider,
			RegisterUserAndSendVerificationUseCase registerUserAndSendVerificationUseCase,
			LoginUserUseCase loginUserUseCase,
			UserRepository userRepository,
			AuthenticateUserWithProviderUseCase authenticateUserWithProviderUseCase,
			GoogleTokenVerifier googleTokenVerifier,
			GoogleOAuthClient googleOAuthClient,
			GoogleOAuthStateService googleOAuthStateService,
			ValidateEmailVerificationUseCase validateEmailVerificationUseCase,
			RequestPasswordResetUseCase requestPasswordResetUseCase,
			ValidatePasswordResetUseCase validatePasswordResetUseCase,
			RefreshTokenCookieManager refreshTokenCookieManager) {
		this.jwtProvider = jwtProvider;
		this.registerUserAndSendVerificationUseCase = registerUserAndSendVerificationUseCase;
		this.loginUserUseCase = loginUserUseCase;
		this.userRepository = userRepository;
		this.authenticateUserWithProviderUseCase = authenticateUserWithProviderUseCase;
		this.googleTokenVerifier = googleTokenVerifier;
		this.googleOAuthClient = googleOAuthClient;
		this.googleOAuthStateService = googleOAuthStateService;
		this.validateEmailVerificationUseCase = validateEmailVerificationUseCase;
		this.requestPasswordResetUseCase = requestPasswordResetUseCase;
		this.validatePasswordResetUseCase = validatePasswordResetUseCase;
		this.refreshTokenCookieManager = refreshTokenCookieManager;
	}

	/*
	 * ===============================
	 * LOGIN (TEMPORAIRE / TECHNIQUE)
	 * ===============================
	 */
	@PostMapping("/login")
	public ResponseEntity<LoginResponse> login(
			@Valid @RequestBody LoginRequest request,
			HttpServletResponse response) {

		// * Récupère les crédentials */
		LoginUserCommand command = new LoginUserCommand(request.getEmail(), request.getPassword());

		// * on exécute la logique de login */
		User user = loginUserUseCase.execute(command);

		// * génère le token */
		String accessToken = jwtProvider.generateAccessToken(
				user.getId().toString(),
				buildAccessClaims(user));

		String refreshToken = jwtProvider.generateRefreshToken(user.getId().toString());

		// * Cookie HttpOnly pour le refresh token */
		refreshTokenCookieManager.set(
				response,
				refreshToken,
				7 * 24 * 3600);

		// Access token retourné au front
		return ResponseEntity.ok(new LoginResponse(accessToken));
	}

	/*
	 * ===============================
	 * LOGIN / REGISTER GOOGLE
	 * ===============================
	 */
	@PostMapping("/google")
	public ResponseEntity<LoginResponse> loginWithGoogle(
			@Valid @RequestBody GoogleLoginRequest request,
			HttpServletResponse response) {

		// 1. Vérifier le token Google (INFRA)
		GoogleUserPayload payload = googleTokenVerifier.verify(request.getIdToken());

		// 2. Construire la commande métier
		RegisterUserCommand command = RegisterUserCommand.oauth(
				AuthProvider.GOOGLE,
				payload.getProviderUserId(),
				payload.getPicture(),
				payload.getEmail(),
				payload.getName());

		// 3. Authentifier (login OU register implicite)
		User user = authenticateUserWithProviderUseCase.execute(command);

		// 4. Générer les tokens (IDENTIQUE AU LOGIN CLASSIQUE)
		String accessToken = jwtProvider.generateAccessToken(
				user.getId().toString(),
				buildAccessClaims(user));

		String refreshToken = jwtProvider.generateRefreshToken(
				user.getId().toString());

		// 5. Cookie refresh token
		refreshTokenCookieManager.set(
				response,
				refreshToken,
				7 * 24 * 3600);

		// 6. Retour access token
		return ResponseEntity.ok(new LoginResponse(accessToken));
	}

	/*
	 * ===============================
	 * GOOGLE OAUTH — URL
	 * ===============================
	 */
	@GetMapping("/google/url")
	public ResponseEntity<Map<String, String>> getGoogleAuthUrl(
			@RequestParam(defaultValue = "web") String source) {
		String state = googleOAuthStateService.generate(source);
		String url = googleOAuthClient.buildAuthorizationUrl(state);
		return ResponseEntity.ok(Map.of("url", url));
	}

	/*
	 * ===============================
	 * GOOGLE OAUTH — CALLBACK
	 * ===============================
	 */
	@GetMapping("/callback/google")
	public void googleCallback(
			@RequestParam String code,
			@RequestParam String state,
			HttpServletResponse response) throws IOException {

		String source = googleOAuthStateService.validateAndGetSource(state);

		String idToken = googleOAuthClient.exchangeCodeForIdToken(code);
		GoogleUserPayload payload = googleTokenVerifier.verify(idToken);

		RegisterUserCommand command = RegisterUserCommand.oauth(
				AuthProvider.GOOGLE,
				payload.getProviderUserId(),
				payload.getPicture(),
				payload.getEmail(),
				payload.getName());

		User user = authenticateUserWithProviderUseCase.execute(command);

		String accessToken = jwtProvider.generateAccessToken(
				user.getId().toString(),
				buildAccessClaims(user));

		String refreshToken = jwtProvider.generateRefreshToken(user.getId().toString());

		refreshTokenCookieManager.set(response, refreshToken, 7 * 24 * 3600);

		String redirectUrl;
		if ("electron".equals(source)) {
			redirectUrl = "tools://auth?token=" + accessToken;
		} else {
			redirectUrl = frontendBaseUrl + "/auth/callback?token=" + accessToken;
		}

		response.sendRedirect(redirectUrl);
	}

	/*
	 * ===============================
	 * REFRESH TOKEN
	 * ===============================
	 */
	@PostMapping("/refresh")
	public Map<String, String> refresh(
			HttpServletRequest request,
			HttpServletResponse response) {

		try {

			Cookie[] cookies = request.getCookies();

			if (cookies == null) {
				logger.warn(
						"AUTH_REFRESH_FAILURE ip={} reason=NO_COOKIE",
						request.getRemoteAddr());
				throw new UserNotFoundException("Utilisateur introuvable");
			}

			String refreshToken = null;
			for (Cookie cookie : cookies) {
				if ("refresh_token".equals(cookie.getName())) {
					refreshToken = cookie.getValue();
					break;
				}
			}

			if (refreshToken == null) {
				logger.warn(
						"AUTH_REFRESH_FAILURE ip={} reason=NO_REFRESH_TOKEN",
						request.getRemoteAddr());
				throw new UserNotFoundException("Utilisateur introuvable");
			}

			// 1. Valider l’ancien refresh token
			Claims claims = jwtProvider.parseToken(refreshToken);
			String tokenType = claims.get("tokenType", String.class);
			if (!"REFRESH".equals(tokenType)) {
				throw new JwtException("Invalid token type");
			}
			Long userId = Long.parseLong(claims.getSubject());

			User user = userRepository
					.findById(userId)
					.orElseThrow(() -> new UserNotFoundException("Utilisateur introuvable"));

			if (!user.isActive()) {
				throw new UserNotFoundException("Utilisateur désactivé");
			}

			// 2. Rotation du refresh token
			Date absoluteExp = claims.getExpiration();
			String newRefreshToken = jwtProvider.generateRefreshToken(user.getId().toString(), absoluteExp);

			long remainingSeconds = (absoluteExp.getTime() - System.currentTimeMillis()) / 1000;

			refreshTokenCookieManager.set(
					response,
					newRefreshToken,
					(int) Math.max(0, remainingSeconds));

			// 3. Génération du nouvel access token
			String newAccessToken = jwtProvider.generateAccessToken(
					user.getId().toString(),
					buildAccessClaims(user));

			logger.info(
					"AUTH_REFRESH_SUCCESS user={} ip={}",
					user.getId().toString(),
					request.getRemoteAddr());

			return Map.of("accessToken", newAccessToken);

		} catch (ExpiredJwtException e) {

			logger.warn(
					"AUTH_REFRESH_FAILURE ip={} reason=REFRESH_EXPIRED",
					request.getRemoteAddr());
			throw e;

		} catch (JwtException | IllegalArgumentException e) {

			logger.warn(
					"AUTH_REFRESH_FAILURE ip={} reason=REFRESH_INVALID",
					request.getRemoteAddr());
			throw e;
		}
	}

	/*
	 * ===============================
	 * LOGOUT
	 * ===============================
	 */
	@PostMapping("/logout")
	public void logout(HttpServletRequest request, HttpServletResponse response) {

		refreshTokenCookieManager.clear(response);
	}

	/*
	 * ===============================
	 * REGISTER
	 * ===============================
	 */
	@PostMapping("/register")
	public ResponseEntity<?> register(@Valid @RequestBody RegisterRequest request) {

		RegisterUserCommand command = RegisterUserCommand.password(
				request.getEmail(),
				request.getName(),
				request.getPassword());

		registerUserAndSendVerificationUseCase.execute(command);

		return ResponseEntity.ok(
				new RegisterResponse("EMAIL_VERIFICATION_REQUIRED",
						"Un email de confirmation vous a été envoyé. Veuillez valider votre adresse pour activer votre compte."));
	}

	private Map<String, Object> buildAccessClaims(User user) {
		return Map.of(
				"tokenType", "ACCESS",
				"userType", user.getUserType().name(),
				"isActive", user.isActive());
	}

	/*
	 * ===============================
	 * VALIDATION EMAIL
	 * ===============================
	 */
	@PostMapping("/verify-email")
	public ResponseEntity<?> verifyEmail(
			@RequestParam("token") String token) {
		validateEmailVerificationUseCase.execute(token);
		return ResponseEntity.ok(
				Map.of(
						"status", "EMAIL_VERIFIED",
						"message", "Adresse email vérifiée avec succès"));
	}

	/*
	 * ===============================
	 * RECOVERY PASSWORD
	 * ===============================
	 */
	@PostMapping("/password/reset-request")
	public ResponseEntity<?> requestPasswordReset(
			@RequestBody PasswordResetRequestDto request) {
		requestPasswordResetUseCase.execute(request.email());
		return ResponseEntity.ok(
				Map.of(
						"status", "RESET_REQUESTED",
						"message", "Si un compte correspondant existe, un email a été envoyé"));
	}

	public record PasswordResetRequestDto(String email) {

	}

	/*
	 * ===============================
	 * CHANGE PASSWORD
	 * ===============================
	 */
	@PostMapping("/password/reset")
	public ResponseEntity<?> resetPassword(
			@RequestBody PasswordResetDto request) {
		validatePasswordResetUseCase.execute(request.token(), request.password());
		return ResponseEntity.ok(
				Map.of(
						"status", "PASSWORD_RESET",
						"message", "Mot de passe modifié avec succès"));
	}

	public record PasswordResetDto(String token, String password) {

	}

	/*
	 * ===============================
	 * LOGIN / REGISTER GITHUB
	 * ===============================
	 */
	/*
	 * @GetMapping("/github/callback")
	 * public void githubCallback(
	 * 
	 * @RequestParam("code") String code,
	 * HttpServletResponse response
	 * ) throws IOException {
	 * 
	 * try {
	 * 
	 * // 1. OAuth exchange
	 * String accessToken = gitHubOAuthClient.exchangeCodeForAccessToken(code);
	 * 
	 * // 2. Fetch GitHub user
	 * GitHubUser gitHubUser = gitHubUserClient.fetchUser(accessToken);
	 * 
	 * // 3. Commande métier
	 * AuthenticateWithProviderCommand command =
	 * new AuthenticateWithProviderCommand(
	 * AuthProvider.GITHUB,
	 * gitHubUser.providerUserId(),
	 * gitHubUser.email(),
	 * gitHubUser.name()
	 * );
	 * 
	 * User user = authenticateUserWithProviderUseCase.execute(command);
	 * 
	 * // 4. Tokens
	 * String accessJwt = jwtProvider.generateAccessToken(
	 * user.getId().toString(),
	 * buildAccessClaims(user)
	 * );
	 * 
	 * String refreshJwt = jwtProvider.generateRefreshToken(
	 * user.getId().toString()
	 * );
	 * 
	 * Cookie refreshCookie = new Cookie("refresh_token", refreshJwt);
	 * refreshCookie.setHttpOnly(true);
	 * refreshCookie.setSecure(cookieProperties.isSecure());
	 * refreshCookie.setPath("/api/v3/auth");
	 * refreshCookie.setMaxAge(7 * 24 * 3600);
	 * refreshCookie.setAttribute("SameSite", "Strict");
	 * response.addCookie(refreshCookie);
	 * 
	 * // SUCCESS → redirect front
	 * response.setHeader("Authorization", "Bearer " + accessJwt);
	 * response.sendRedirect(frontendBaseUrl);
	 * 
	 * } catch (UserDisabledException e) {
	 * 
	 * response.sendRedirect(frontendBaseUrl + "/login?error=USER_DISABLED");
	 * 
	 * } catch (Exception e) {
	 * 
	 * response.sendRedirect(frontendBaseUrl + "/login?error=GITHUB_AUTH_FAILED");
	 * }
	 * }
	 */

	/*
	 * ===============================
	 * ELECTRON SESSION
	 * ===============================
	 */
	@PostMapping("/electron/session")
	public ResponseEntity<Void> createElectronSession(HttpServletResponse response) {
		String userId = SecurityContextHolder.getContext().getAuthentication().getName();
		String refreshToken = jwtProvider.generateRefreshToken(userId);
		refreshTokenCookieManager.set(response, refreshToken, 7 * 24 * 3600);
		return ResponseEntity.ok().build();
	}
}
