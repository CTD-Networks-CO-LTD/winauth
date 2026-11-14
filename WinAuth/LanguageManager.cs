using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.Json;

namespace WinAuth
{
	public static class LanguageManager
	{
		public const string Japanese = "Japanese";
		public const string English = "English";
		public const string RegistryLanguageKey = WinAuthHelper.WINAUTHREGKEY + @"\Language";

		public static readonly string[] SupportedLanguages = new[]
		{
			Japanese,
			English
		};

		private static readonly object _lock = new object();
		private static readonly Dictionary<string, Dictionary<string, string>> _cache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		private static string _currentLanguage = English;

		public static event EventHandler LanguageChanged;

		public static string CurrentLanguage
		{
			get
			{
				lock (_lock)
				{
					return _currentLanguage;
				}
			}
		}

		public static string NormalizeLanguage(string language)
		{
			if (string.IsNullOrWhiteSpace(language))
			{
				return null;
			}

			string trimmed = language.Trim();
			foreach (string supported in SupportedLanguages)
			{
				if (string.Equals(trimmed, supported, StringComparison.OrdinalIgnoreCase))
				{
					return supported;
				}
			}

			return null;
		}

		public static string DetectSystemLanguage(CultureInfo uiCulture = null)
		{
			CultureInfo culture = uiCulture;
			if (culture == null)
			{
				try
				{
					culture = CultureInfo.CurrentUICulture;
				}
				catch (Exception)
				{
					culture = null;
				}
			}
			if (culture == null)
			{
				try
				{
					culture = CultureInfo.InstalledUICulture;
				}
				catch (Exception)
				{
					culture = null;
				}
			}

			if (culture != null && string.Equals(culture.TwoLetterISOLanguageName, "ja", StringComparison.OrdinalIgnoreCase))
			{
				return Japanese;
			}

			return English;
		}

		public static string ResolveInitialLanguage(string savedLanguage = null, CultureInfo uiCulture = null)
		{
			if (!string.IsNullOrWhiteSpace(savedLanguage))
			{
				return NormalizeLanguage(savedLanguage) ?? English;
			}

			return DetectSystemLanguage(uiCulture);
		}

		public static string Initialize(string savedLanguage = null, CultureInfo uiCulture = null)
		{
			string resolved = ResolveInitialLanguage(savedLanguage, uiCulture);
			lock (_lock)
			{
				_currentLanguage = resolved;
			}
			GetDictionary(resolved);
			if (!string.Equals(resolved, English, StringComparison.OrdinalIgnoreCase))
			{
				GetDictionary(English);
			}
			return resolved;
		}

		public static string SetLanguage(string language, bool raiseEvent = true)
		{
			string normalized = NormalizeLanguage(language) ?? English;
			bool changed;
			lock (_lock)
			{
				changed = !string.Equals(_currentLanguage, normalized, StringComparison.Ordinal);
				_currentLanguage = normalized;
			}

			GetDictionary(normalized);

			if (raiseEvent && changed)
			{
				LanguageChanged?.Invoke(null, EventArgs.Empty);
			}

			return normalized;
		}

		public static void ClearCache()
		{
			lock (_lock)
			{
				_cache.Clear();
			}
		}

		public static string GetString(string name)
		{
			return GetString(name, CurrentLanguage);
		}

		public static string GetString(string name, string language)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}

			string targetLang = NormalizeLanguage(language) ?? English;
			var dict = GetDictionary(targetLang);
			if (dict != null && dict.TryGetValue(name, out string value) && value != null)
			{
				return value;
			}

			if (!string.Equals(targetLang, English, StringComparison.OrdinalIgnoreCase))
			{
				var fallbackDict = GetDictionary(English);
				if (fallbackDict != null && fallbackDict.TryGetValue(name, out string fallbackValue) && fallbackValue != null)
				{
					return fallbackValue;
				}
			}

			return null;
		}

		public static Dictionary<string, string> GetDictionary(string language, string baseDirectory = null)
		{
			string normalized = NormalizeLanguage(language) ?? English;
			bool useCache = string.IsNullOrEmpty(baseDirectory);

			if (useCache)
			{
				lock (_lock)
				{
					if (_cache.TryGetValue(normalized, out var cached))
					{
						return cached;
					}
				}
			}

			Dictionary<string, string> loaded = null;

			try
			{
				string rootDir = string.IsNullOrEmpty(baseDirectory) ? AppContext.BaseDirectory : baseDirectory;
				if (!string.IsNullOrEmpty(rootDir))
				{
					string filePath = Path.Combine(rootDir, "Language", normalized + ".json");
					if (File.Exists(filePath))
					{
						string json = File.ReadAllText(filePath, Encoding.UTF8);
						loaded = ParseJsonDictionary(json);
					}
				}
			}
			catch (Exception)
			{
				loaded = null;
			}

			if (loaded == null)
			{
				try
				{
					string resourceName = "WinAuth.Language." + normalized + ".json";
					using (var stream = typeof(LanguageManager).Assembly.GetManifestResourceStream(resourceName))
					{
						if (stream != null)
						{
							using (var reader = new StreamReader(stream, Encoding.UTF8))
							{
								string json = reader.ReadToEnd();
								loaded = ParseJsonDictionary(json);
							}
						}
					}
				}
				catch (Exception)
				{
					loaded = null;
				}
			}

			loaded = loaded ?? new Dictionary<string, string>(StringComparer.Ordinal);

			if (useCache)
			{
				lock (_lock)
				{
					_cache[normalized] = loaded;
				}
			}

			return loaded;
		}

		private static Dictionary<string, string> ParseJsonDictionary(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				return null;
			}

			var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
			if (parsed == null)
			{
				return null;
			}

			return new Dictionary<string, string>(parsed, StringComparer.Ordinal);
		}
	}

	public class CustomResourceManager : ResourceManager
	{
		public CustomResourceManager(string baseName, Assembly assembly)
			: base(baseName, assembly)
		{
		}

		public override string GetString(string name)
		{
			return GetString(name, null);
		}

		public override string GetString(string name, CultureInfo culture)
		{
			string jsonValue = LanguageManager.GetString(name);
			if (jsonValue != null)
			{
				return jsonValue;
			}

			try
			{
				return base.GetString(name, culture);
			}
			catch (MissingManifestResourceException)
			{
				return null;
			}
		}
	}
}
