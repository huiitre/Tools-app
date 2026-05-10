package fr.huiitre.tools.modules.core.security.infrastructure;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import javax.crypto.Cipher;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import java.security.SecureRandom;
import java.util.Base64;

/**
 * Service de chiffrement AES-256-GCM pour les données sensibles en base de données.
 * Utilise une clé maître injectée via variable d'environnement.
 */
@Service
public class EncryptionService {

    private static final String ALGORITHM = "AES/GCM/NoPadding";
    private static final int TAG_LENGTH_BIT = 128;
    private static final int IV_LENGTH_BYTE = 12;

    private final SecretKey secretKey;

    public EncryptionService(@Value("${tools.security.encryption.master-key}") String masterKeyBase64) {
        try {
            byte[] decodedKey = Base64.getDecoder().decode(masterKeyBase64);
            this.secretKey = new SecretKeySpec(decodedKey, "AES");
        } catch (Exception e) {
            throw new RuntimeException("Impossible d'initialiser le service de chiffrement. Vérifiez la clé maître (BASE64).", e);
        }
    }

    /**
     * Chiffre un texte clair et retourne le résultat en Base64.
     *
     * @param plainText Texte à chiffrer
     * @param ivBase64 Vecteur d'initialisation (Base64)
     * @return Texte chiffré en Base64
     */
    public String encrypt(String plainText, String ivBase64) {
        if (plainText == null) return null;
        try {
            byte[] iv = Base64.getDecoder().decode(ivBase64);
            Cipher cipher = Cipher.getInstance(ALGORITHM);
            cipher.init(Cipher.ENCRYPT_MODE, secretKey, new GCMParameterSpec(TAG_LENGTH_BIT, iv));
            byte[] cipherText = cipher.doFinal(plainText.getBytes());
            return Base64.getEncoder().encodeToString(cipherText);
        } catch (Exception e) {
            throw new RuntimeException("Erreur lors du chiffrement des données", e);
        }
    }

    /**
     * Déchiffre un texte chiffré (Base64) en utilisant l'IV fourni.
     *
     * @param cipherTextBase64 Texte chiffré (Base64)
     * @param ivBase64 Vecteur d'initialisation (Base64)
     * @return Texte clair
     */
    public String decrypt(String cipherTextBase64, String ivBase64) {
        if (cipherTextBase64 == null) return null;
        try {
            byte[] cipherText = Base64.getDecoder().decode(cipherTextBase64);
            byte[] iv = Base64.getDecoder().decode(ivBase64);
            Cipher cipher = Cipher.getInstance(ALGORITHM);
            cipher.init(Cipher.DECRYPT_MODE, secretKey, new GCMParameterSpec(TAG_LENGTH_BIT, iv));
            byte[] plainText = cipher.doFinal(cipherText);
            return new String(plainText);
        } catch (Exception e) {
            throw new RuntimeException("Erreur lors du déchiffrement des données. Clé ou IV invalide.", e);
        }
    }

    /**
     * Génère un nouvel IV aléatoire (12 octets) pour une nouvelle ligne de données.
     *
     * @return IV en Base64
     */
    public String generateIv() {
        byte[] iv = new byte[IV_LENGTH_BYTE];
        new SecureRandom().nextBytes(iv);
        return Base64.getEncoder().encodeToString(iv);
    }
}
