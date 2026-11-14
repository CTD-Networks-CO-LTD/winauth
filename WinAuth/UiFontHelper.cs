using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace WinAuth
{
	public static class UiFontHelper
	{
		private static readonly string[] UiFontCandidates = new[]
		{
			"Yu Gothic UI",
			"Meiryo UI",
			"Segoe UI"
		};

		private static readonly string[] MonoFontCandidates = new[]
		{
			"Consolas",
			"Yu Gothic UI",
			"Meiryo UI",
			"Segoe UI"
		};

		private static readonly HashSet<string> MonoControlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"codeField",
			"secretKeyField",
			"serialNumberField",
			"restoreCodeField",
			"newRestoreCodeField",
			"newLoginCodeField",
			"newSerialNumberField",
			"revocationcodeField",
			"revocationcode2Field",
			"steamdataField",
			"deviceidField",
			"deviceIdField",
			"detailsTextBox",
			"errorTextBox",
			"errorBox",
			"diagnosticTextBox"
		};

		private static string _uiFontFamilyName;
		private static string _monoFontFamilyName;

		public static string GetUiFontFamilyName()
		{
			if (_uiFontFamilyName == null)
			{
				_uiFontFamilyName = ResolveInstalledFont(UiFontCandidates) ?? SystemFonts.DefaultFont.FontFamily.Name;
			}
			return _uiFontFamilyName;
		}

		public static string GetMonoFontFamilyName()
		{
			if (_monoFontFamilyName == null)
			{
				_monoFontFamilyName = ResolveInstalledFont(MonoFontCandidates) ?? GetUiFontFamilyName();
			}
			return _monoFontFamilyName;
		}

		private static string ResolveInstalledFont(string[] candidates)
		{
			try
			{
				using (var installed = new InstalledFontCollection())
				{
					var available = new HashSet<string>(installed.Families.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
					foreach (string candidate in candidates)
					{
						if (available.Contains(candidate))
						{
							return candidate;
						}
					}
				}
			}
			catch (Exception)
			{
			}
			return null;
		}

		public static Font CreateUiFont(float sizeInPoints, FontStyle style = FontStyle.Regular)
		{
			return new Font(GetUiFontFamilyName(), sizeInPoints, style, GraphicsUnit.Point);
		}

		public static Font CreateMonoFont(float sizeInPoints, FontStyle style = FontStyle.Regular)
		{
			return new Font(GetMonoFontFamilyName(), sizeInPoints, style, GraphicsUnit.Point);
		}

		public static Font CreateOtpCodeFont(float sizeInPoints = 13.5f, FontStyle style = FontStyle.Bold)
		{
			string family = ResolveInstalledFont(new[] { "Segoe UI", "Yu Gothic UI", "Meiryo UI" }) ?? GetUiFontFamilyName();
			return new Font(family, sizeInPoints, style, GraphicsUnit.Point);
		}

		public static bool IsSecretOrCodeControl(Control control)
		{
			if (control == null || string.IsNullOrEmpty(control.Name))
			{
				return false;
			}
			return MonoControlNames.Contains(control.Name);
		}

		public static void ApplyFormFonts(Form form)
		{
			if (form == null)
			{
				return;
			}

			ApplyControlFontsRecursive(form);
		}

		private static void ApplyControlFontsRecursive(Control parent)
		{
			foreach (Control c in parent.Controls)
			{
				if (c is AuthenticatorListBox)
				{
					continue;
				}

				if (c.ContextMenuStrip != null)
				{
					ApplyContextMenuFont(c.ContextMenuStrip);
				}

				if (ShouldPreserveFont(c) == false)
				{
					float size = (c.Font != null && c.Font.SizeInPoints > 0f) ? c.Font.SizeInPoints : 9f;
					FontStyle style = c.Font != null ? c.Font.Style : FontStyle.Regular;

					if (IsSecretOrCodeControl(c))
					{
						c.Font = CreateMonoFont(Math.Max(size, 9f), style);
					}
					else if (IsLegacyFont(c.Font) || c is RichTextBox || c is TextBox || c is ComboBox || c is ListBox)
					{
						c.Font = CreateUiFont(Math.Max(size, 9f), style);
					}
				}

				if (c.Controls != null && c.Controls.Count > 0)
				{
					ApplyControlFontsRecursive(c);
				}
			}
		}

		internal static bool ShouldPreserveFont(Control control)
		{
			if (control == null)
			{
				return true;
			}

			if (string.Equals(control.GetType().Name, "MetroFormButton", StringComparison.Ordinal))
			{
				return true;
			}

			if (control.Font != null && string.Equals(control.Font.FontFamily?.Name, "Webdings", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			string ns = control.GetType().Namespace;
			if (!string.IsNullOrEmpty(ns) && ns.StartsWith("MetroFramework.Controls", StringComparison.Ordinal))
			{
				return true;
			}

			return false;
		}

		private static bool IsLegacyFont(Font font)
		{
			if (font == null || font.FontFamily == null)
			{
				return true;
			}

			string name = font.FontFamily.Name;
			return string.Equals(name, "Microsoft Sans Serif", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(name, "Arial", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(name, "Segoe UI", StringComparison.OrdinalIgnoreCase);
		}

		public static void ApplyContextMenuFont(ContextMenuStrip menu)
		{
			if (menu == null)
			{
				return;
			}

			Font uiFont = CreateUiFont(9f, FontStyle.Regular);
			menu.Font = uiFont;
			ApplyToolStripItemsFont(menu.Items, uiFont);
		}

		private static void ApplyToolStripItemsFont(ToolStripItemCollection items, Font font)
		{
			if (items == null)
			{
				return;
			}

			foreach (ToolStripItem item in items)
			{
				item.Font = font;
				if (item is ToolStripDropDownItem dropDownItem && dropDownItem.DropDownItems != null && dropDownItem.DropDownItems.Count > 0)
				{
					ApplyToolStripItemsFont(dropDownItem.DropDownItems, font);
				}
			}
		}
	}
}
