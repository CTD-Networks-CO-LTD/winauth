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
            // When native DLL is not present or embedded, CreateInstance/Init must throw proper exception rather than crash
            Assert.Throws<LibraryNotFoundException>(() =>
            {
                YubiKey.CreateInstance();
            });
        }
    }
}