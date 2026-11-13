using System;
using System.Text;
using Xunit;
using WinAuth;

namespace AuthenticatorTests
{
    public class RfcTests
    {
        [Fact]
        public void TestBase32EncodeDecode()
        {
            var base32 = Base32.getInstance();
            byte[] raw = Encoding.ASCII.GetBytes("Hello, World!");
            string encoded = base32.Encode(raw);
            byte[] decoded = base32.Decode(encoded);
            Assert.Equal("Hello, World!", Encoding.ASCII.GetString(decoded));
        }

        [Fact]
        public void TestGoogleAuthenticatorCodeCalculation()
        {
            var auth = new GoogleAuthenticator();
            // Test secret "12345678901234567890" in Base32: GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ
            auth.Enroll("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ");
            // Set fixed ServerTimeDiff for interval 1 (30 seconds)
            auth.ServerTimeDiff = (1 * 30000L) - GoogleAuthenticator.CurrentTime;
            Assert.Equal(1L, auth.CodeInterval);
            string code = auth.CurrentCode;
            Assert.NotNull(code);
            Assert.Equal(6, code.Length);
        }

        [Fact]
        public void TestHOTPAuthenticator()
        {
            var hotp = new HOTPAuthenticator();
            hotp.SecretKey = Encoding.ASCII.GetBytes("12345678901234567890");
            // WinAuth increments counter on each code calculation
            hotp.Counter = -1;
            string code0 = hotp.CurrentCode;
            Assert.Equal("755224", code0);
            Assert.Equal(0, hotp.Counter);

            string code1 = hotp.CurrentCode;
            Assert.Equal("287082", code1);
            Assert.Equal(1, hotp.Counter);

            string code2 = hotp.CurrentCode;
            Assert.Equal("359152", code2);
            Assert.Equal(2, hotp.Counter);
        }

        [Fact]
        public void TestGoogleAuthenticatorOtpauthUrl()
        {
            var auth = new GoogleAuthenticator();
            auth.Enroll("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ");
            Assert.Equal(6, auth.CodeDigits);
            Assert.Equal(30, auth.Period);
        }
    }
}