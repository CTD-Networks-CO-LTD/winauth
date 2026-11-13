using System;
using System.Text.RegularExpressions;
using Xunit;
using WinAuth;

namespace AuthenticatorTests
{
    public class Phase4SecurityTests
    {
        [Fact]
        public void TestHtmlEncodeSingleQuote()
        {
            string input = "<script>alert('xss');</script>";
            string encoded = WinAuthHelper.HtmlEncode(input);
            Assert.Contains("&#39;", encoded);
            Assert.DoesNotContain("'", encoded);
            Assert.Contains("&lt;", encoded);
            Assert.Contains("&gt;", encoded);
        }

        [Fact]
        public void TestRegexReDosProtectionInIssuer()
        {
            // Verify regex escaping handles special regex characters in issuer safely
            string issuer = "[Special] (Company) +Auth*";
            string label = "[Special] (Company) +Auth* (user@example.com)";

            string escapedIssuer = Regex.Escape(issuer);
            Match match = Regex.Match(label, @"^" + escapedIssuer + @"\s+\((.*?)\)(.*)", RegexOptions.None, TimeSpan.FromMilliseconds(500));
            Assert.True(match.Success);
            Assert.Equal("user@example.com", match.Groups[1].Value);
        }

        [Fact]
        public void TestYubiKeyLoadMissingResourceThrowsLibraryNotFoundException()
        {
            string libraryName = (IntPtr.Size == 4 ? "WinAuth.YubiKey.x86.dll" : "WinAuth.YubiKey.x64.dll");
            using (var resourceStream = typeof(Authenticator).Assembly.GetManifestResourceStream("WinAuth.Resources." + libraryName))
            {
                if (resourceStream == null)
                {
                    // When native DLL is not embedded, CreateInstance/Init must throw proper exception rather than crash
                    Assert.Throws<LibraryNotFoundException>(() =>
                    {
                        YubiKey.CreateInstance();
                    });
                }
                else
                {
                    // When native DLL is embedded (e.g. CI release build), loading and querying status must succeed without crashing
                    using (var yubiKey = YubiKey.CreateInstance())
                    {
                        Assert.NotNull(yubiKey);
                    }
                }
            }
        }

        [Fact]
        public void TestAuthenticatorEmbeddedResourcesUseWinAuthRootNamespace()
        {
            var names = typeof(Authenticator).Assembly.GetManifestResourceNames();
            Assert.Contains("WinAuth.Properties.Resources.resources", names);
        }
    }
}