using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using WinAuth;

namespace AuthenticatorTests
{
    public class Phase3CryptoTests
    {
        [Fact]
        public void TestWinAuth4EncryptionDecryptionWithPassword()
        {
            string plaintext = "MySecretDataString1234567890!@#$%^&*()";
            string hexPlain = Authenticator.ByteArrayToString(Encoding.UTF8.GetBytes(plaintext));
            string password = "TestMasterPassword!2026";

            // Encrypt using WINAUTH4
            string encrypted = Authenticator.EncryptSequence(hexPlain, Authenticator.PasswordTypes.Explicit, password, null);
            Assert.StartsWith(Authenticator.ENCRYPTION_HEADER_V4, encrypted);

            // Decrypt using WINAUTH4
            string decryptedHex = Authenticator.DecryptSequence(encrypted, Authenticator.PasswordTypes.Explicit, password, null);
            string decryptedText = Encoding.UTF8.GetString(Authenticator.StringToByteArray(decryptedHex));

            Assert.Equal(plaintext, decryptedText);
        }

        [Fact]
        public void TestWinAuth4WrongPasswordFails()
        {
            string hexPlain = Authenticator.ByteArrayToString(Encoding.UTF8.GetBytes("SecretData"));
            string password = "CorrectPassword123";
            string wrongPassword = "WrongPassword456";

            string encrypted = Authenticator.EncryptSequence(hexPlain, Authenticator.PasswordTypes.Explicit, password, null);
            Assert.Throws<BadPasswordException>(() =>
            {
                Authenticator.DecryptSequence(encrypted, Authenticator.PasswordTypes.Explicit, wrongPassword, null);
            });
        }

        [Fact]
        public void TestWinAuth4TamperingDetected()
        {
            string hexPlain = Authenticator.ByteArrayToString(Encoding.UTF8.GetBytes("DataToProtectAgainstTampering"));
            string password = "StrongPassword789";

            string encrypted = Authenticator.EncryptSequence(hexPlain, Authenticator.PasswordTypes.Explicit, password, null);
            char[] chars = encrypted.ToCharArray();

            // Tamper with one character in ciphertext/tag
            chars[chars.Length - 5] = (chars[chars.Length - 5] == 'A') ? 'B' : 'A';
            string tampered = new string(chars);

            Assert.Throws<BadPasswordException>(() =>
            {
                Authenticator.DecryptSequence(tampered, Authenticator.PasswordTypes.Explicit, password, null);
            });
        }

        [Fact]
        public void TestHeaderRemovalDoesNotBypassIntegrity()
        {
            string hexPlain = Authenticator.ByteArrayToString(Encoding.UTF8.GetBytes("HeaderRemovalTest"));
            string password = "Password123";

            string encrypted = Authenticator.EncryptSequence(hexPlain, Authenticator.PasswordTypes.Explicit, password, null);

            // Remove WINAUTH4 header
            string noHeader = encrypted.Substring(Authenticator.ENCRYPTION_HEADER_V4.Length);

            // Must throw BadPasswordException and NOT fallback to unauthenticated decryption
            Assert.Throws<BadPasswordException>(() =>
            {
                Authenticator.DecryptSequence(noHeader, Authenticator.PasswordTypes.Explicit, password, null);
            });
        }

        [Fact]
        public void TestWinAuth3BackwardCompatibility()
        {
            string plaintext = "LegacyWinAuth3ProtectedData";
            string hexPlain = Authenticator.ByteArrayToString(Encoding.UTF8.GetBytes(plaintext));
            string password = "LegacyPassword2015";

            // Encrypt using legacy WINAUTH3 format
            string legacyEncrypted = Authenticator.EncryptSequenceV3(hexPlain, Authenticator.PasswordTypes.Explicit, password, null);
            Assert.StartsWith(Authenticator.ENCRYPTION_HEADER, legacyEncrypted);

            // Decrypt using DecryptSequence (which should transparently recognize WINAUTH3 and decrypt)
            string decryptedHex = Authenticator.DecryptSequence(legacyEncrypted, Authenticator.PasswordTypes.Explicit, password, null);
            string decryptedText = Encoding.UTF8.GetString(Authenticator.StringToByteArray(decryptedHex));

            Assert.Equal(plaintext, decryptedText);
        }

        [Fact]
        public void TestSafeHasherDoesNotDowngradeToSha1()
        {
            using (var hasher = Authenticator.SafeHasher("UNKNOWN"))
            {
                Assert.IsAssignableFrom<SHA256>(hasher);
                Assert.Equal(256, hasher.HashSize);
            }
        }
    }
}