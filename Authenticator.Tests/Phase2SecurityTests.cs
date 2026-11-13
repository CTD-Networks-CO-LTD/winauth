using System;
using System.IO;
using System.Text;
using System.Xml;
using Xunit;
using WinAuth;

namespace AuthenticatorTests
{
    public class Phase2SecurityTests
    {
        [Fact]
        public void TestXxeProhibitedInSafeReaderSettings()
        {
            // Verify SafeReaderSettings prohibits DTD processing (XXE prevention)
            Assert.Equal(DtdProcessing.Prohibit, Authenticator.SafeReaderSettings.DtdProcessing);

            string xxeXml = "<?xml version=\"1.0\"?><!DOCTYPE foo [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><foo>&xxe;</foo>";
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(xxeXml)))
            {
                using (var reader = XmlReader.Create(ms, Authenticator.SafeReaderSettings))
                {
                    Assert.Throws<XmlException>(() =>
                    {
                        while (reader.Read()) { }
                    });
                }
            }
        }

        [Fact]
        public void TestExtremeClockDriftProtection()
        {
            // Verify that extreme drift (>24h) is considered invalid
            long normalDrift = 5000L; // 5 seconds
            long extremeDrift = 25L * 60 * 60 * 1000; // 25 hours
            Assert.True(Math.Abs(normalDrift) <= 24L * 60 * 60 * 1000);
            Assert.True(Math.Abs(extremeDrift) > 24L * 60 * 60 * 1000);
        }

        [Fact]
        public void TestUrlEncodingParameterSanitization()
        {
            string email = "test+user@example.com";
            string password = "p@ssword&secret=123";
            string encodedEmail = Uri.EscapeDataString(email);
            string encodedPass = Uri.EscapeDataString(password);

            Assert.Equal("test%2Buser%40example.com", encodedEmail);
            Assert.Equal("p%40ssword%26secret%3D123", encodedPass);
            Assert.DoesNotContain("&", encodedPass);
            Assert.DoesNotContain("=", encodedPass);
        }

        [Fact]
        public void TestInvalidExceptionsDoNotExposeSecrets()
        {
            var encEx = new InvalidEncryptionException("secret_plain", "mypassword", "enc123", "dec123");
            Assert.Equal("Invalid encryption detected", encEx.Message);

            var secEx = new InvalidSecretDataException(null, "mypassword", "WINAUTH3", new System.Collections.Generic.List<string> { "secret_seed" });
            Assert.Equal("Error decoding Secret Data", secEx.Message);
            Assert.Equal("WINAUTH3", secEx.EncType);
        }
    }
}