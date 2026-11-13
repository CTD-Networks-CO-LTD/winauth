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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.Web;

using WinAuth.Resources;

namespace WinAuth
{
	/// <summary>
	/// General error report form
	/// </summary>
	public partial class ExceptionForm : WinAuth.ResourceForm
	{
		/// <summary>
		/// Exception that caused the error report
		/// </summary>
		public Exception ErrorException { get; set; }

		/// <summary>
		/// Current config
		/// </summary>
		public WinAuthConfig Config { get; set; }

		/// <summary>
		/// Create the  Form
		/// </summary>
		public ExceptionForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Load the error report form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ExceptionForm_Load(object sender, EventArgs e)
		{
			errorIcon.Image = SystemIcons.Error.ToBitmap();
			this.Height = detailsButton.Top + detailsButton.Height + 45;

			this.errorLabel.Text = string.Format(this.errorLabel.Text, (ErrorException != null ? ErrorException.Message : strings.UnknownError));

			// build data
			dataText.Text = BuildDiagnostics();
		}

		/// <summary>
		/// Build a diagnostics string for the current Config and any exception that had been thrown
		/// </summary>
		/// <returns>diagnostics information</returns>
		private string BuildDiagnostics()
		{
			StringBuilder diag = new StringBuilder();

			Version version;
#if NETFX_4
			if (Version.TryParse(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion, out version) == true)
			{
				diag.Append("Version: " + version.ToString(4)).Append(Environment.NewLine);
			}
#endif
			diag.Append("OS: " + Environment.OSVersion.ToString()).Append(Environment.NewLine);
			diag.Append(".NET: " + Environment.Version.ToString()).Append(Environment.NewLine).Append(Environment.NewLine);

			// add the exception
			if (ErrorException != null)
			{
				diag.Append("--EXCEPTION--").Append(Environment.NewLine);

				Exception ex = ErrorException;
				while (ex != null)
				{
					diag.Append("Type: ").Append(ex.GetType().FullName).Append(Environment.NewLine);
					diag.Append("Message: ").Append(ex.Message).Append(Environment.NewLine);
					diag.Append("Stack: ").Append(new System.Diagnostics.StackTrace(ex).ToString()).Append(Environment.NewLine);
					ex = ex.InnerException;
				}
				if (ErrorException is InvalidSecretDataException)
				{
					diag.Append("EncType: " + ((InvalidSecretDataException)ErrorException).EncType).Append(Environment.NewLine);
				}
			}

			return diag.ToString();
		}

		/// <summary>
		/// Click the Quit button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void quitButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Click the Continue button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void continueButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Click to show the details
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void detailsButton_Click(object sender, EventArgs e)
		{
			dataText.Visible = !dataText.Visible;
			if (dataText.Visible == true)
			{
				this.detailsButton.Text = strings.HideDetails;
				this.Height += 160;
			}
			else
			{
				this.detailsButton.Text = strings._ExceptionForm_detailsButton_;
				this.Height -= 160;
			}
		}

	}
}
