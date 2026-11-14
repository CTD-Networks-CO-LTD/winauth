using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WinAuth;
using Xunit;

namespace AuthenticatorTests
{
    public class BugFixTests
    {
        private static void RunInSta(Action action)
        {
            Exception? ex = null;
            var thread = new Thread(() =>
            {
                try
                {
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (ex != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
            }
        }

        #region 不具合1: MetroForm のウィンドウボタン配置テスト

        [Fact]
        public void MetroForm_AboutForm_ButtonsPositionedAtTopRight()
        {
            RunInSta(() =>
            {
                using var form = new AboutForm();
                form.Show();

                Button? closeBtn = null;
                Button? minBtn = null;
                Button? maxBtn = null;

                foreach (Control ctrl in form.Controls)
                {
                    if (ctrl is Button btn)
                    {
                        if (btn.Text == "r") closeBtn = btn;
                        else if (btn.Text == "0") minBtn = btn;
                        else if (btn.Text == "1" || btn.Text == "2") maxBtn = btn;
                    }
                }

                Assert.NotNull(closeBtn);
                Assert.True(closeBtn.Location.X > 0, $"Close button X is {closeBtn.Location.X}, expected > 0");
                Assert.True(closeBtn.Location.X + closeBtn.Width <= form.ClientSize.Width,
                    $"Close button right edge {closeBtn.Location.X + closeBtn.Width} exceeds ClientSize.Width {form.ClientSize.Width}");

                int originalWidth = form.Width;
                form.Width = originalWidth - 140;

                Assert.True(closeBtn.Location.X > 0, $"After shrink, Close button X is {closeBtn.Location.X}, expected > 0");
                Assert.True(closeBtn.Location.X + closeBtn.Width <= form.ClientSize.Width,
                    $"After shrink, Close button right edge exceeds ClientSize.Width");

                form.Width = originalWidth + 100;
                Assert.True(closeBtn.Location.X > 0);
                Assert.True(closeBtn.Location.X + closeBtn.Width <= form.ClientSize.Width);
            });
        }

        [Fact]
        public void MetroForm_UpdateCheckForm_ButtonsPositionedAtTopRight()
        {
            RunInSta(() =>
            {
                using var form = new UpdateCheckForm();
                form.Show();

                Button? closeBtn = null;
                foreach (Control ctrl in form.Controls)
                {
                    if (ctrl is Button btn && btn.Text == "r")
                    {
                        closeBtn = btn;
                        break;
                    }
                }

                Assert.NotNull(closeBtn);
                Assert.True(closeBtn.Location.X > 0);
                Assert.True(closeBtn.Location.X + closeBtn.Width <= form.ClientSize.Width);

                form.Width = 350;
                Assert.True(closeBtn.Location.X > 0);
                Assert.True(closeBtn.Location.X + closeBtn.Width <= form.ClientSize.Width);
            });
        }

        #endregion
        #region 不具合2: バージョン警告および設定読み込みテスト

        [Fact]
        public void LoadConfig_ReadOnlyV35Config_DoesNotSetUpgradeFlagOrOverwriteCreated()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string xmlPath = Path.Combine(tempDir, "winauth.xml");
                DateTime expectedCreated = new DateTime(2020, 5, 10, 12, 0, 0, DateTimeKind.Utc);
                long createdMs = Convert.ToInt64((expectedCreated - new DateTime(1970, 1, 1)).TotalMilliseconds);
                string xmlContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<WinAuth version=""3.5"">
  <WinAuthAuthenticator>
    <name>TestAuth</name>
    <created>{createdMs}</created>
    <type>Google</type>
    <secretdata>JBSWY3DPEHPK3PXP</secretdata>
  </WinAuthAuthenticator>
</WinAuth>";
                File.WriteAllText(xmlPath, xmlContent);
                File.SetAttributes(xmlPath, FileAttributes.ReadOnly);

                var config = WinAuthHelper.LoadConfig(null, xmlPath);

                Assert.True(config.IsReadOnly, "Config should be recognized as read-only.");
                Assert.False(config.Upgraded, "Upgraded flag should remain false for read-only config.");
                Assert.Single(config);
                Assert.Equal(expectedCreated, config[0].Created.ToUniversalTime());
            }
            finally
            {
                if (File.Exists(Path.Combine(tempDir, "winauth.xml")))
                {
                    File.SetAttributes(Path.Combine(tempDir, "winauth.xml"), FileAttributes.Normal);
                }
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void LoadConfig_Version40AndNewer_LoadsSuccessfullyWithoutException()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // version 4.0
                string xml40 = Path.Combine(tempDir, "v40.xml");
                File.WriteAllText(xml40, @"<?xml version=""1.0"" encoding=""utf-8""?>
<WinAuth version=""4.0"">
  <WinAuthAuthenticator>
    <name>Test40</name>
    <type>Google</type>
    <secretdata>JBSWY3DPEHPK3PXP</secretdata>
  </WinAuthAuthenticator>
</WinAuth>");
                var config40 = WinAuthHelper.LoadConfig(null, xml40);
                Assert.NotNull(config40);
                Assert.Equal(4.0m, config40.Version);
                Assert.Single(config40);

                // version 4.5 (上位互換バージョン): 例外にならず読み込めて、安全のため IsReadOnly になる
                string xmlNewer = Path.Combine(tempDir, "v45.xml");
                File.WriteAllText(xmlNewer, @"<?xml version=""1.0"" encoding=""utf-8""?>
<WinAuth version=""4.5"">
  <WinAuthAuthenticator>
    <name>Test45</name>
    <type>Google</type>
    <secretdata>JBSWY3DPEHPK3PXP</secretdata>
  </WinAuthAuthenticator>
</WinAuth>");
                var configNewer = WinAuthHelper.LoadConfig(null, xmlNewer);
                Assert.NotNull(configNewer);
                Assert.Equal(4.5m, configNewer.Version);
                Assert.True(configNewer.IsReadOnly, "Higher version config should be treated as read-only to prevent corruption.");
                Assert.Single(configNewer);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void LoadConfig_RelativePath_NormalizesToFullPath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string originalCurrentDir = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = tempDir;
                string xmlName = "relative_winauth.xml";
                File.WriteAllText(Path.Combine(tempDir, xmlName), @"<?xml version=""1.0"" encoding=""utf-8""?>
<WinAuth version=""4.0"">
</WinAuth>");

                var config = WinAuthHelper.LoadConfig(null, xmlName);
                Assert.NotNull(config);
                Assert.True(Path.IsPathRooted(config.Filename), $"config.Filename '{config.Filename}' should be an absolute path.");
                Assert.Equal(Path.GetFullPath(Path.Combine(tempDir, xmlName)), config.Filename);
            }
            finally
            {
                Environment.CurrentDirectory = originalCurrentDir;
                Directory.Delete(tempDir, true);
            }
        }

        #endregion

        #region 不具合3: アセンブリバージョン・メタデータ検証

        [Fact]
        public void AssemblyMetadata_VersionIs40()
        {
            Assert.Equal(4.0m, WinAuthConfig.CURRENTVERSION);

            var winAuthAsm = typeof(WinAuthForm).Assembly;
            var ver = winAuthAsm.GetName().Version;
            Assert.NotNull(ver);
            Assert.Equal(4, ver.Major);
            Assert.Equal(0, ver.Minor);

            var authAsm = typeof(WinAuthAuthenticator).Assembly;
            var authVer = authAsm.GetName().Version;
            Assert.NotNull(authVer);
            Assert.Equal(4, authVer.Major);
            Assert.Equal(0, authVer.Minor);
        }

        #endregion

        #region ポータブル版初期設定ファイル（winauth.xml）読込検証

        [Fact]
        public void LoadConfig_MinimalPortableConfig_LoadsSuccessfullyWithoutError()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthPortableTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                string xmlPath = Path.Combine(tempDir, WinAuthHelper.DEFAULT_AUTHENTICATOR_FILE_NAME);
                string curVerStr = WinAuthConfig.CURRENTVERSION.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                string minimalXml = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<WinAuth version=\"{0}\">\n</WinAuth>\n", curVerStr);
                File.WriteAllText(xmlPath, minimalXml, new System.Text.UTF8Encoding(false));

                var config = WinAuthHelper.LoadConfig(null, xmlPath);
                Assert.NotNull(config);
                Assert.Equal(WinAuthConfig.CURRENTVERSION, config.Version);
                Assert.Empty(config);
                Assert.False(config.IsReadOnly, "Minimal portable config matching CURRENTVERSION should not be read-only.");
                Assert.True(config.AutoSize);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        #endregion

    }
}
