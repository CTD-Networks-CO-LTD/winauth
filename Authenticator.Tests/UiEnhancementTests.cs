using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using MetroFramework;
using WinAuth;
using WinAuth.Resources;
using Xunit;

namespace AuthenticatorTests
{
    public class UiEnhancementTests
    {
        private static void RunInSta(Action action)
        {
            Exception? ex = null;
            var thread = new Thread(() =>
            {
                string originalLanguage = LanguageManager.CurrentLanguage;
                try
                {
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    LanguageManager.SetLanguage(originalLanguage, false);
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

        [Fact]
        public void LanguageJson_ContainsAllRequiredKeysAndUpdatedCopyright()
        {
            var en = LanguageManager.GetDictionary(LanguageManager.English);
            var ja = LanguageManager.GetDictionary(LanguageManager.Japanese);

            Assert.NotNull(en);
            Assert.NotNull(ja);
            Assert.True(en.Count >= 130, $"Expected >= 130 keys in English.json, actual {en.Count}");
            Assert.Equal(en.Count, ja.Count);

            foreach (string key in en.Keys)
            {
                Assert.True(ja.ContainsKey(key), $"Japanese.json is missing key '{key}'");
                Assert.False(string.IsNullOrWhiteSpace(en[key]), $"English.json key '{key}' is empty");
                Assert.False(string.IsNullOrWhiteSpace(ja[key]), $"Japanese.json key '{key}' is empty");
            }

            Assert.Contains("MenuLanguage", en.Keys);
            Assert.Contains("LanguageJapanese", en.Keys);
            Assert.Contains("LanguageEnglish", en.Keys);
            Assert.Contains("Copyright © 2013-2026 Colin Mackie / Modified 2026 CTD Networks Co., Ltd.", en["_AboutForm_aboutLabel_"]);
            Assert.Contains("Copyright © 2013-2026 Colin Mackie / Modified 2026 CTD Networks Co., Ltd.", ja["_AboutForm_aboutLabel_"]);
        }

        [Theory]
        [InlineData("ja-JP", "Japanese")]
        [InlineData("ja", "Japanese")]
        [InlineData("en-US", "English")]
        [InlineData("en-GB", "English")]
        [InlineData("fr-FR", "English")]
        [InlineData("de-DE", "English")]
        [InlineData("zh-CN", "English")]
        [InlineData("ko-KR", "English")]
        public void LanguageManager_DetectSystemLanguage_FollowsCultureAndFallbacksToEnglish(string cultureName, string expected)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            string actual = LanguageManager.DetectSystemLanguage(culture);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void LanguageManager_ResolveInitialLanguage_PrioritizesSavedSettingAndFallbacksInvalid()
        {
            var jaCulture = CultureInfo.GetCultureInfo("ja-JP");
            var enCulture = CultureInfo.GetCultureInfo("en-US");
            var frCulture = CultureInfo.GetCultureInfo("fr-FR");

            Assert.Equal("Japanese", LanguageManager.ResolveInitialLanguage("Japanese", enCulture));
            Assert.Equal("English", LanguageManager.ResolveInitialLanguage("English", jaCulture));
            Assert.Equal("English", LanguageManager.ResolveInitialLanguage("French", jaCulture));
            Assert.Equal("English", LanguageManager.ResolveInitialLanguage("..\\..\\bad", jaCulture));

            Assert.Equal("Japanese", LanguageManager.ResolveInitialLanguage(null, jaCulture));
            Assert.Equal("English", LanguageManager.ResolveInitialLanguage("", frCulture));
        }

        [Fact]
        public void LanguageManager_CustomResourceManager_FallbacksToEnglishOnMissingKey()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthLangTest_" + Guid.NewGuid().ToString("N"));
            string langDir = Path.Combine(tempDir, "Language");
            Directory.CreateDirectory(langDir);
            try
            {
                File.WriteAllText(Path.Combine(langDir, "Japanese.json"), "{\"MenuAbout\":\"バージョン情報テスト\"}");
                var partialDict = LanguageManager.GetDictionary(LanguageManager.Japanese, tempDir);
                Assert.Single(partialDict);
                Assert.Equal("バージョン情報テスト", partialDict["MenuAbout"]);

                // Built-in fallback check
                LanguageManager.SetLanguage(LanguageManager.Japanese, false);
                Assert.Equal("バージョン情報", WinAuthMain.StringResources.GetString("MenuAbout"));
                Assert.Equal("About", LanguageManager.GetString("MenuAbout", "UnknownLanguage"));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void WinAuthForm_OptionsMenu_HasLanguageSubmenuBetweenAboutAndExit()
        {
            RunInSta(() =>
            {
                LanguageManager.SetLanguage(LanguageManager.English, false);
                var config = new WinAuthConfig();
                using var form = new WinAuthForm(config);
                form.Show();

                var items = form.OptionsMenuControl.Items.Cast<ToolStripItem>().ToList();
                int aboutIndex = items.FindIndex(i => i.Name == "aboutOptionsMenuItem");
                int langIndex = items.FindIndex(i => i.Name == "languageOptionsMenuItem");
                int exitIndex = items.FindIndex(i => i.Name == "exitOptionsMenuItem");

                Assert.True(aboutIndex >= 0);
                Assert.True(langIndex > aboutIndex, "Language menu should appear after About...");
                Assert.True(exitIndex > langIndex, "Language menu should appear before Exit");

                var langItem = Assert.IsType<ToolStripMenuItem>(items[langIndex]);
                Assert.Equal("Language", langItem.Text);
                Assert.Equal(2, langItem.DropDownItems.Count);
                Assert.Equal("Japanese", langItem.DropDownItems[0].Text);
                Assert.Equal("English", langItem.DropDownItems[1].Text);

                form.TriggerOpeningOptionsMenu();
                Assert.False(((ToolStripMenuItem)langItem.DropDownItems[0]).Checked);
                Assert.True(((ToolStripMenuItem)langItem.DropDownItems[1]).Checked);

                form.SetUiLanguage(LanguageManager.Japanese, persistToRegistry: false);
                form.TriggerOpeningOptionsMenu();
                langItem = Assert.IsType<ToolStripMenuItem>(form.OptionsMenuControl.Items.Cast<ToolStripItem>().First(i => i.Name == "languageOptionsMenuItem"));
                Assert.Equal("言語 (Language)", langItem.Text);
                Assert.True(((ToolStripMenuItem)langItem.DropDownItems[0]).Checked);
                Assert.False(((ToolStripMenuItem)langItem.DropDownItems[1]).Checked);

                form.Close();
            });
        }

        [Fact]
        public void WinAuthForm_ManualLanguageSwitch_UpdatesUiImmediatelyAndPersistsXml()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthLangPersist_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string xmlPath = Path.Combine(tempDir, "winauth.xml");
            File.WriteAllText(xmlPath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<WinAuth version=""4.0"">
</WinAuth>");

            try
            {
                RunInSta(() =>
                {
                    LanguageManager.SetLanguage(LanguageManager.English, false);
                    var config = WinAuthHelper.LoadConfig(null, xmlPath);
                    using var form = new WinAuthForm(config);
                    form.Show();

                    var addBtn = form.Controls.Find("addAuthenticatorButton", true).First();
                    var introLbl = form.Controls.Find("introLabel", true).First();
                    Assert.Equal("Add", addBtn.Text);

                    form.SetUiLanguage(LanguageManager.Japanese, persistToRegistry: false);
                    Assert.Equal("追加", addBtn.Text);
                    Assert.Contains("「追加」", introLbl.Text);
                    Assert.Equal("Japanese", config.Language);

                    form.SetUiLanguage(LanguageManager.English, persistToRegistry: false);
                    Assert.Equal("Add", addBtn.Text);
                    Assert.Equal("English", config.Language);

                    form.SetUiLanguage(LanguageManager.Japanese, persistToRegistry: false);
                    form.Close();
                });

                string savedXml = File.ReadAllText(xmlPath);
                Assert.Contains("<language>Japanese</language>", savedXml);

                string preReadLang = WinAuthHelper.ReadLanguageFromConfigFile(xmlPath);
                Assert.Equal("Japanese", preReadLang);
                Assert.Equal("Japanese", LanguageManager.ResolveInitialLanguage(preReadLang, CultureInfo.GetCultureInfo("en-US")));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void WinAuthForm_ReadOnlyConfig_LanguageSwitchDoesNotOverwriteFile()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthReadOnlyLang_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string xmlPath = Path.Combine(tempDir, "winauth.xml");
            string initialXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<WinAuth version=""4.5"">
</WinAuth>";
            File.WriteAllText(xmlPath, initialXml);

            try
            {
                RunInSta(() =>
                {
                    LanguageManager.SetLanguage(LanguageManager.English, false);
                    var config = WinAuthHelper.LoadConfig(null, xmlPath);
                    Assert.True(config.IsReadOnly);

                    using var form = new WinAuthForm(config);
                    form.Show();
                    form.SetUiLanguage(LanguageManager.Japanese, persistToRegistry: false);
                    Assert.Equal("Japanese", LanguageManager.CurrentLanguage);
                    form.Close();
                });

                Assert.Equal(initialXml, File.ReadAllText(xmlPath));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void WinAuthConfig_EncryptedConfig_ReadsLanguageBeforeDecryption()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WinAuthEncLang_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string xmlPath = Path.Combine(tempDir, "winauth.xml");

            try
            {
                var config = new WinAuthConfig();
                config.Filename = xmlPath;
                config.Language = LanguageManager.Japanese;
                config.PasswordType = Authenticator.PasswordTypes.Explicit;
                config.Password = "TestPass123!";
                var auth = new WinAuthAuthenticator
                {
                    Name = "TestAccount",
                    AuthenticatorData = new GoogleAuthenticator()
                };
                ((GoogleAuthenticator)auth.AuthenticatorData).Enroll("JBSWY3DPEHPK3PXP");
                config.Add(auth);
                WinAuthHelper.SaveConfig(config);

                string preRead = WinAuthHelper.ReadLanguageFromConfigFile(xmlPath);
                Assert.Equal("Japanese", preRead);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void AboutForm_DisplaysUpdatedCopyrightAndSufficientHeight()
        {
            RunInSta(() =>
            {
                foreach (string lang in new[] { LanguageManager.English, LanguageManager.Japanese })
                {
                    LanguageManager.SetLanguage(lang, false);
                    using var form = new AboutForm();
                    form.Show();

                    var aboutLabel = form.Controls.Find("aboutLabel", true).First();
                    Assert.Contains("Copyright © 2013-2026 Colin Mackie / Modified 2026 CTD Networks Co., Ltd.", aboutLabel.Text);
                    Assert.Contains(WinAuthHelper.GetVersionString(), aboutLabel.Text);
                    Assert.True(aboutLabel.Height >= 60, $"aboutLabel height {aboutLabel.Height} is too small for 3 lines");
                    form.Close();
                }
            });
        }

        [Fact]
        public void WinAuthForm_VersionLabel_PositionedBelowButtonsAndTracksResize()
        {
            RunInSta(() =>
            {
                LanguageManager.SetLanguage(LanguageManager.English, false);
                var config = new WinAuthConfig { AutoSize = false, Width = 420, Height = 260 };
                using var form = new WinAuthForm(config);
                form.Show();

                var versionLabel = form.VersionLabelControl;
                Assert.NotNull(versionLabel);
                Assert.True(versionLabel.Visible);
                Assert.Equal(WinAuthHelper.GetVersionDisplayString(), versionLabel.Text);
                Assert.True(versionLabel.Top >= 25, $"versionLabel.Top={versionLabel.Top} should be below window buttons");
                Assert.True(versionLabel.Left >= 128, $"versionLabel.Left={versionLabel.Left} should not overlap WinAuth title");
                Assert.True(versionLabel.Right <= form.ClientSize.Width);

                form.Width = 300;
                Assert.True(versionLabel.Left >= 128);
                Assert.True(versionLabel.Right <= form.ClientSize.Width);

                form.Width = 800;
                Assert.True(versionLabel.Right <= form.ClientSize.Width);
                Assert.True(versionLabel.Right >= form.ClientSize.Width - 20);

                form.SetUiLanguage(LanguageManager.Japanese, persistToRegistry: false);
                Assert.Equal(WinAuthHelper.GetVersionDisplayString(), versionLabel.Text);

                form.Close();
            });
        }

        [Fact]
        public void Fonts_HighVisibilityAppliedAndWebdingsPreservedAndListItemFitsHeight()
        {
            RunInSta(() =>
            {
                string uiFontName = UiFontHelper.GetUiFontFamilyName();
                string monoFontName = UiFontHelper.GetMonoFontFamilyName();
                Assert.False(string.IsNullOrEmpty(uiFontName));
                Assert.False(string.IsNullOrEmpty(monoFontName));

                using var metroDefault = MetroFonts.Default(12f);
                using var metroLight = MetroFonts.DefaultLight(12f);
                Assert.Equal(uiFontName, metroDefault.FontFamily.Name, ignoreCase: true);
                Assert.Equal(uiFontName, metroLight.FontFamily.Name, ignoreCase: true);

                var config = new WinAuthConfig();
                using var mainForm = new WinAuthForm(config);
                mainForm.Show();

                Button? closeBtn = mainForm.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "r");
                Assert.NotNull(closeBtn);
                Assert.True(UiFontHelper.ShouldPreserveFont(closeBtn));
                Assert.True(closeBtn.Location.X > 0 && closeBtn.Right <= mainForm.ClientSize.Width);
                Assert.Equal(uiFontName, mainForm.OptionsMenuControl.Font.FontFamily.Name, ignoreCase: true);

                var gAuth = new GoogleAuthenticator();
                gAuth.Enroll("JBSWY3DPEHPK3PXP");
                using var secretForm = new ShowSecretKeyForm { CurrentAuthenticator = new WinAuthAuthenticator { Name = "Sample", AuthenticatorData = gAuth } };
                secretForm.Show();
                var secretField = secretForm.Controls.Find("secretKeyField", true).First();
                Assert.Equal(monoFontName, secretField.Font.FontFamily.Name, ignoreCase: true);

                using var aboutForm = new AboutForm();
                aboutForm.Show();
                var licenseBox = aboutForm.Controls.Find("richTextBox1", true).First();
                Assert.Equal(uiFontName, licenseBox.Font.FontFamily.Name, ignoreCase: true);

                using var bmp = new Bitmap(400, 100);
                using var g = Graphics.FromImage(bmp);
                using var nameFont = UiFontHelper.CreateUiFont(10.5f, FontStyle.Regular);
                using var codeFont = UiFontHelper.CreateOtpCodeFont(13f, FontStyle.Bold);
                float totalContentHeight = 5f + g.MeasureString("日本語テストアカウント", nameFont).Height + 0f + g.MeasureString("123 456", codeFont).Height;
                Assert.True(totalContentHeight <= mainForm.AuthenticatorListControl.ItemHeight,
                    $"Content height {totalContentHeight} exceeds ItemHeight {mainForm.AuthenticatorListControl.ItemHeight}");

                secretForm.Close();
                aboutForm.Close();
                mainForm.Close();
            });
        }

        [Fact]
        public void AutoSize_DefaultIsTrueAndCheckedInOptionsMenu()
        {
            RunInSta(() =>
            {
                LanguageManager.SetLanguage(LanguageManager.Japanese, false);
                var config = new WinAuthConfig();
                Assert.True(config.AutoSize);

                using var form = new WinAuthForm(config);
                form.Show();
                form.TriggerOpeningOptionsMenu();

                var autoSizeItem = Assert.IsType<ToolStripMenuItem>(
                    form.OptionsMenuControl.Items.Cast<ToolStripItem>().First(i => i.Name == "autoSizeOptionsMenuItem"));
                Assert.True(autoSizeItem.Checked, "autoSizeOptionsMenuItem should be checked by default.");

                form.Close();
            });
        }

        [Fact]
        public void AddMenu_And_UpdateCheckForm_LocalizedInJapanese()
        {
            RunInSta(() =>
            {
                LanguageManager.SetLanguage(LanguageManager.Japanese, false);
                var config = new WinAuthConfig();
                using var mainForm = new WinAuthForm(config);
                mainForm.Show();

                // Verify Add Authenticator dropdown menu is localized
                var addItems = mainForm.AddAuthenticatorMenuControl.Items.Cast<ToolStripItem>().ToList();
                var firstItem = Assert.IsType<ToolStripMenuItem>(addItems.First(i => i.Name == "addAuthenticatorMenuItem_0"));
                Assert.Equal("汎用認証器 (TOTP/HOTP)", firstItem.Text);
                var importItem = Assert.IsType<ToolStripMenuItem>(addItems.First(i => i.Name == "importTextMenuItem"));
                Assert.Equal("インポート...", importItem.Text);

                // Verify UpdateCheckForm ("更新の確認...") is localized
                using var updateForm = new UpdateCheckForm { Config = config };
                updateForm.Show();
                Assert.Equal("更新の確認", updateForm.Text);
                var autoLabel = updateForm.Controls.Find("autoLabel", true).First();
                Assert.Equal("WinAuth の起動時に自動的に更新を確認する", autoLabel.Text);
                var cancelButton = updateForm.Controls.Find("cancelButton", true).First();
                Assert.Equal("キャンセル", cancelButton.Text);
                var autoDropdown = Assert.IsType<MetroFramework.Controls.MetroComboBox>(
                    updateForm.Controls.Find("autoDropdown", true).First());
                Assert.Equal("毎回", autoDropdown.Items[0]?.ToString());
                updateForm.Close();

                // Verify AddAuthenticator form is localized and MetroTextBox inputs are not polluted with form title
                using var addForm = new AddAuthenticator { Authenticator = new WinAuthAuthenticator() };
                addForm.Show();
                Assert.Equal("認証器の追加", addForm.Text);
                var verifyBtn = addForm.Controls.Find("verifyButton", true).First();
                Assert.Equal("認証器を検証", verifyBtn.Text);
                var secretCodeField = Assert.IsType<MetroFramework.Controls.MetroTextBox>(
                    addForm.Controls.Find("secretCodeField", true).First());
                Assert.Equal(string.Empty, secretCodeField.Text);
                foreach (Control inner in secretCodeField.Controls)
                {
                    Assert.NotEqual(addForm.Text, inner.Text);
                }
                addForm.Close();

                mainForm.Close();
            });
        }
    }
}
