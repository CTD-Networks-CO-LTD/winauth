/*
 * Copyright (C) 2013 Colin Mackie.
 * This software is distributed under the terms of the GNU General Public License.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using System.Windows;
using System.Windows.Forms;

using WinAuth.Resources;

namespace WinAuth
{
	/// <summary>
	/// Class holding the latest version information
	/// </summary>
	public class WinAuthVersionInfo
	{
		/// <summary>
		/// Version number
		/// </summary>
		public Version Version;

		/// <summary>
		/// Date of release
		/// </summary>
		public DateTime Released;

		/// <summary>
		/// URL for download
		/// </summary>
		public string Url;

		/// <summary>
		/// Optional changes
		/// </summary>
		public string Changes;
		/// <summary>
		/// SHA-256 hash of the release package for verification (VULN-15)
		/// </summary>
		public string Sha256;


		/// <summary>
		/// Create the new version instance
		/// </summary>
		/// <param name="version"></param>
		public WinAuthVersionInfo(Version version)
		{
			Version = version;
		}
	}

	/// <summary>
	/// Class to check for newer version of WinAuth
	/// </summary>
	public class WinAuthUpdater
	{
		/// <summary>
		/// Period when the poller thread will check if it needs to check for a new version
		/// </summary>
		protected const int UPDATECHECKTHREAD_SLEEP = 6 * 60 * 60 * 1000; // 6 hrs to check if we need to check

		/// <summary>
		/// Registry key value name for when we last checked for a new version
		/// </summary>
		protected const string WINAUTHREGKEY_LASTCHECK = WinAuthHelper.WINAUTHREGKEY + "\\LastUpdateCheck";

		/// <summary>
		/// Registry key value name for how often we check for a new version
		/// </summary>
		protected const string WINAUTHREGKEY_CHECKFREQUENCY = WinAuthHelper.WINAUTHREGKEY + "\\UpdateCheckFrequency";

		/// <summary>
		/// Registry key value name for the last version we found when we checked
		/// </summary>
#if BETA
		protected const string WINAUTHREGKEY_LATESTVERSION = WinAuthHelper.WINAUTHREGKEY + "\\LatestBetaVersion";
#else
		protected const string WINAUTHREGKEY_LATESTVERSION = WinAuthHelper.WINAUTHREGKEY + "\\LatestVersion";
#endif

		/// <summary>
		/// The interval for checking new versions. Null is never, Zero is each time, else a period.
		/// </summary>
		private TimeSpan? _autocheckInterval;

		/// <summary>
		/// The last known new version
		/// </summary>
		private Version _latestVersion;

		/// <summary>
		/// When we last checked for a new version
		/// </summary>
		private DateTime _lastCheck;

		/// <summary>
		/// Current Config
		/// </summary>
		protected WinAuthConfig Config { get; set; }

		/// <summary>
		/// Create the version checker instance
		/// </summary>
		public WinAuthUpdater(WinAuthConfig config)
		{
			Config = config;

			if (Config != null)
			{
				// read the update interval and last known latest version from the registry
				TimeSpan interval;
				if (TimeSpan.TryParse(Config.ReadSetting(WINAUTHREGKEY_CHECKFREQUENCY, string.Empty), out interval) == true)
				{
					_autocheckInterval = interval;
				}

				long lastCheck = 0;
				if (long.TryParse(Config.ReadSetting(WINAUTHREGKEY_LASTCHECK, null), out lastCheck) == true)
				{
					_lastCheck = new DateTime(lastCheck);
				}

				Version version;
#if NETFX_4
				if (Version.TryParse(Config.ReadSetting(WINAUTHREGKEY_LATESTVERSION, string.Empty), out version) == true)
				{
					_latestVersion = version;
				}
#endif
#if NETFX_3
				try
				{
					version = new Version(Config.ReadSetting(WINAUTHREGKEY_LATESTVERSION, string.Empty));
					_latestVersion = version;
				}
				catch (Exception) { }
#endif
			}
		}

		#region Properties

		/// <summary>
		/// Get when the last check was done
		/// </summary>
		public DateTime LastCheck
		{
			get
			{
				return _lastCheck;
			}
		}

		/// <summary>
		/// Get the last known latest version or null
		/// </summary>
		public Version LastKnownLatestVersion
		{
			get
			{
				return _latestVersion;
			}
			protected set
			{
				_latestVersion = value;
			}
		}

		/// <summary>
		/// Shared HttpClient for update checks (TLS 1.2/1.3)
		/// </summary>
		private static readonly HttpClient _httpClient = CreateHttpClient();

		private static HttpClient CreateHttpClient()
		{
			var client = new HttpClient
			{
				Timeout = TimeSpan.FromSeconds(15)
			};
			return client;
		}

		/// <summary>
		/// Optional feed provider override for offline unit testing.
		/// </summary>
		internal Func<string> OverrideFeedProvider { get; set; }

		/// <summary>
		/// Normalize a Version to 3 components (Major.Minor.Build) for reliable comparison.
		/// </summary>
		public static Version NormalizeVersion(Version version)
		{
			if (version == null)
			{
				return null;
			}
			int build = version.Build >= 0 ? version.Build : 0;
			return new Version(version.Major, version.Minor, build);
		}

		/// <summary>
		/// Parse a version tag such as "4.1.1" or "v4.1.1-beta" into a normalized Version.
		/// </summary>
		internal static bool TryParseReleaseVersion(string tag, out Version version)
		{
			version = null;
			if (string.IsNullOrWhiteSpace(tag))
			{
				return false;
			}

			var match = Regex.Match(tag.Trim(), @"^[vV]?(\d+\.\d+\.\d+(?:\.\d+)?)");
			if (!match.Success)
			{
				return false;
			}

			if (Version.TryParse(match.Groups[1].Value, out Version parsed))
			{
				version = NormalizeVersion(parsed);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Get the current version
		/// </summary>
		public Version CurrentVersion
		{
			get
			{
				if (TryParseReleaseVersion(WinAuthHelper.GetVersionString(), out Version ver))
				{
					return ver;
				}
				Version asmVer = typeof(WinAuthUpdater).Assembly.GetName().Version;
				if (asmVer != null)
				{
					return NormalizeVersion(asmVer);
				}
				throw new InvalidOperationException("Cannot get Assembly version information");
			}
		}

		/// <summary>
		/// Get flag if we have autochecking enabled
		/// </summary>
		public bool IsAutoCheck
		{
			get
			{
				return (_autocheckInterval != null);
			}
		}

		/// <summary>
		/// Get the interval between checks
		/// </summary>
		public TimeSpan? UpdateInterval
		{
			get
			{
				return _autocheckInterval;
			}
		}

#endregion

		/// <summary>
		/// Start an AutoCheck thread that will periodically check for a new version and make a callback
		/// </summary>
		/// <param name="callback">Callback when a new version is found</param>
		public void AutoCheck(Action<Version> callback)
		{
			// create a thread to check for latest version
			Thread thread = new Thread(new ParameterizedThreadStart(AutoCheckPoller));
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Start(callback);
		}

		/// <summary>
		/// AutoCheck thread method to poll for new version
		/// </summary>
		/// <param name="p">our callback</param>
		protected virtual void AutoCheckPoller(object p)
		{
			Action<Version> callback = p as Action<Version>;

			do
			{
				// only if autochecking is on, and is due, and we don't already have a later version
				if (this.IsAutoCheck == true
					&& _autocheckInterval.HasValue && _lastCheck.Add(_autocheckInterval.Value) < DateTime.Now
					&& (LastKnownLatestVersion == null || NormalizeVersion(LastKnownLatestVersion) <= this.CurrentVersion))
				{
					// update the last check time
					_lastCheck = DateTime.Now;
					Config?.WriteSetting(WINAUTHREGKEY_LASTCHECK, _lastCheck.Ticks.ToString());

					// check for latest version
					try
					{
						var latest = GetLatestVersion();
						if (latest != null && NormalizeVersion(latest.Version) > this.CurrentVersion)
						{
							callback?.Invoke(latest.Version);
						}
					}
					catch (Exception) { }
				}

				Thread.Sleep(UPDATECHECKTHREAD_SLEEP);

			} while (true);
		}

		/// <summary>
		/// Explicitly get the latest version information. Will be asynchronous if a callback is provided.
		/// </summary>
		/// <param name="callback">optional callback for async operation</param>
		/// <returns>latest WinAuthVersionInfo or null if async</returns>
		public virtual WinAuthVersionInfo GetLatestVersion(Action<WinAuthVersionInfo, bool, Exception> callback = null)
		{
			if (OverrideFeedProvider != null)
			{
				try
				{
					string mockResult = OverrideFeedProvider();
					WinAuthVersionInfo latestVersion = !string.IsNullOrWhiteSpace(mockResult)
						? ParseGetLatestVersion(mockResult)
						: null;
					if (latestVersion != null)
					{
						LastKnownLatestVersion = latestVersion.Version;
						Config?.WriteSetting(WINAUTHREGKEY_LATESTVERSION, latestVersion.Version.ToString(3));
					}
					callback?.Invoke(latestVersion, false, null);
					return latestVersion;
				}
				catch (Exception ex)
				{
					callback?.Invoke(null, false, ex);
					return null;
				}
			}

			// get the update URL from the config else use the default
			string updateUrl = WinAuthMain.WINAUTH_UPDATE_URL;
			try
			{
				var settings = new System.Configuration.AppSettingsReader();
				string appvalue = settings.GetValue("UpdateCheckUrl", typeof(string)) as string;
				if (string.IsNullOrEmpty(appvalue) == false)
				{
					updateUrl = appvalue;
				}
			}
			catch (Exception) { }

			if (string.IsNullOrEmpty(updateUrl))
			{
				if (callback != null)
				{
					callback(null, false, null);
				}
				return null;
			}
			try
			{
				if (callback == null)
				{
					string result = FetchUpdateFeedString(updateUrl);
					WinAuthVersionInfo latestVersion = ParseGetLatestVersion(result);
					if (latestVersion != null)
					{
						LastKnownLatestVersion = latestVersion.Version;
						Config?.WriteSetting(WINAUTHREGKEY_LATESTVERSION, latestVersion.Version.ToString(3));
					}
					return latestVersion;
				}
				else
				{
					System.Threading.Tasks.Task.Run(() =>
					{
						try
						{
							string result = FetchUpdateFeedString(updateUrl);
							WinAuthVersionInfo latestVersion = ParseGetLatestVersion(result);
							if (latestVersion != null)
							{
								LastKnownLatestVersion = latestVersion.Version;
								Config?.WriteSetting(WINAUTHREGKEY_LATESTVERSION, latestVersion.Version.ToString(3));
							}
							callback(latestVersion, false, null);
						}
						catch (Exception ex)
						{
							callback(null, false, ex);
						}
					});
					return null;
				}
			}
			catch (Exception ex)
			{
				callback?.Invoke(null, false, ex);
				return null;
			}
		}

		private string FetchUpdateFeedString(string updateUrl)
		{
			using (var request = new HttpRequestMessage(HttpMethod.Get, updateUrl))
			{
				request.Headers.TryAddWithoutValidation("User-Agent", "WinAuth-" + this.CurrentVersion.ToString());
				request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
				using (var response = _httpClient.Send(request))
				{
					response.EnsureSuccessStatusCode();
					return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				}
			}
		}

		/// <summary>
		/// Parse the returned GitHub Releases JSON or legacy XML to extract version information.
		/// </summary>
		/// <param name="result">version JSON or XML information</param>
		/// <returns>new WinAuthVersionInfo object</returns>
		internal WinAuthVersionInfo ParseGetLatestVersion(string result)
		{
			if (string.IsNullOrWhiteSpace(result))
			{
				throw new InvalidOperationException("Invalid return data");
			}

			string trimmed = result.TrimStart();
			if (trimmed.StartsWith("[", StringComparison.Ordinal) || trimmed.StartsWith("{", StringComparison.Ordinal))
			{
				return ParseGitHubReleasesJson(trimmed);
			}

			// load xml document and pull out nodes (XXE prevention - VULN-11)
			XmlDocument xml = new XmlDocument();
			xml.XmlResolver = null;
			xml.LoadXml(result);
			var node = xml.SelectSingleNode("//version");

			Version version = null;
			if (node != null)
			{
				TryParseReleaseVersion(node.InnerText, out version);
			}
			if (node != null && version != null)
			{
				WinAuthVersionInfo latestversion = new WinAuthVersionInfo(version);

				DateTime released;
				node = xml.SelectSingleNode("//released");
				if (node != null && DateTime.TryParse(node.InnerText, out released) == true)
				{
					latestversion.Released = released;
				}
				node = xml.SelectSingleNode("//url");
				if (node != null && string.IsNullOrEmpty(node.InnerText) == false)
				{
					latestversion.Url = node.InnerText;
				}
				node = xml.SelectSingleNode("//changes");
				if (node != null && string.IsNullOrEmpty(node.InnerText) == false)
				{
					latestversion.Changes = node.InnerText;
				}
				node = xml.SelectSingleNode("//sha256");
				if (node != null && string.IsNullOrEmpty(node.InnerText) == false)
				{
					latestversion.Sha256 = node.InnerText.Trim();
				}

				return latestversion;
			}
			else
			{
				throw new InvalidOperationException("Invalid return data");
			}
		}

		private WinAuthVersionInfo ParseGitHubReleasesJson(string json)
		{
			using (JsonDocument doc = JsonDocument.Parse(json))
			{
				JsonElement root = doc.RootElement;
				WinAuthVersionInfo best = null;

				if (root.ValueKind == JsonValueKind.Array)
				{
					foreach (JsonElement item in root.EnumerateArray())
					{
						WinAuthVersionInfo candidate = ExtractGitHubReleaseElement(item);
						if (candidate != null && (best == null || candidate.Version > best.Version))
						{
							best = candidate;
						}
					}
				}
				else if (root.ValueKind == JsonValueKind.Object)
				{
					best = ExtractGitHubReleaseElement(root);
				}

				return best;
			}
		}

		private static WinAuthVersionInfo ExtractGitHubReleaseElement(JsonElement item)
		{
			if (item.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			if (item.TryGetProperty("draft", out JsonElement draftProp) && draftProp.ValueKind == JsonValueKind.True)
			{
				return null;
			}

			if (!item.TryGetProperty("tag_name", out JsonElement tagProp) || tagProp.ValueKind != JsonValueKind.String)
			{
				return null;
			}

			if (!TryParseReleaseVersion(tagProp.GetString(), out Version version))
			{
				return null;
			}

			var info = new WinAuthVersionInfo(version);

			if (item.TryGetProperty("html_url", out JsonElement htmlUrlProp) && htmlUrlProp.ValueKind == JsonValueKind.String)
			{
				info.Url = htmlUrlProp.GetString();
			}
			if (item.TryGetProperty("published_at", out JsonElement publishedProp) && publishedProp.ValueKind == JsonValueKind.String
				&& DateTime.TryParse(publishedProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime published))
			{
				info.Released = published;
			}
			if (item.TryGetProperty("body", out JsonElement bodyProp) && bodyProp.ValueKind == JsonValueKind.String)
			{
				info.Changes = bodyProp.GetString();
			}

			return info;
		}

		/// <summary>
		/// Set the interval for automatic update checks. Null is disabled. Zero is every time.
		/// </summary>
		/// <param name="interval">new interval or null to disable</param>
		public void SetUpdateInterval(TimeSpan? interval)
		{
			// get the next check time
			if (Config != null)
			{
				if (interval != null)
				{
					// write into registry
					Config.WriteSetting(WINAUTHREGKEY_CHECKFREQUENCY, string.Format("{0}:{1:00}:{2:00}", interval.Value.TotalHours, interval.Value.Minutes, interval.Value.Seconds)); // toString("c") is Net4

					// if last update not set, set to now
					if (Config.ReadSetting(WINAUTHREGKEY_LASTCHECK) == null)
					{
						Config.WriteSetting(WINAUTHREGKEY_LASTCHECK, DateTime.Now.Ticks.ToString());
					}
				}
				else
				{
					// remove from registry
					Config.WriteSetting(WINAUTHREGKEY_CHECKFREQUENCY, null);
				}
			}

			// update local values
			_autocheckInterval = interval;
		}
	}
}
