using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Portals;

namespace forDNN.Modules.UsersExportImport
{
	public class CommonController
	{
		#region Export functonality

		public static string ToXML(DataTable dt)
		{
			System.Xml.XmlDocument objDoc = new System.Xml.XmlDocument();
			System.Xml.XmlElement objRoot = objDoc.CreateElement("users");
			foreach (DataRow dr in dt.Rows)
			{
				System.Xml.XmlElement objUser = objDoc.CreateElement("user");
				foreach (DataColumn dc in dt.Columns)
				{
					System.Xml.XmlAttribute objAttr = objDoc.CreateAttribute(dc.ColumnName);
					objAttr.Value = string.Format("{0}", dr[dc.ColumnName]);
					objUser.Attributes.Append(objAttr);
				}
				objRoot.AppendChild(objUser);
			}
			objDoc.AppendChild(objRoot);
			return objDoc.InnerXml;
		}

		public static string ToCSV(DataTable dt, bool includeHeaderAsFirstRow, string separator)
		{
			StringBuilder csvRows = new StringBuilder();
			StringBuilder sb = null;
			int ColumnsCount = dt.Columns.Count;

			if (includeHeaderAsFirstRow)
			{
				sb = new StringBuilder();
				for (int index = 0; index < ColumnsCount; index++)
				{
					if (dt.Columns[index].ColumnName != null)
						sb.Append(dt.Columns[index].ColumnName);

					if (index < ColumnsCount - 1)
						sb.Append(separator);
				}
				csvRows.AppendLine(sb.ToString());
			}

			foreach(DataRow dr in dt.Rows)
			{
				sb = new StringBuilder();
				for (int index = 0; index < ColumnsCount; index++)
				{
					string value = string.Format("{0}", dr[index]);
					if (dt.Columns[index].DataType == typeof(String))
					{
						//If double quotes are used in value, ensure each are replaced but 2.
						if (value.IndexOf("\"") >= 0)
							value = value.Replace("\"", "\"\"");

						//If separtor are is in value, ensure it is put in double quotes.
						if (value.IndexOf(separator) >= 0)
							value = "\"" + value + "\"";
					}
					sb.Append(value);

					if (index < ColumnsCount - 1)
						sb.Append(separator);
				}

				csvRows.AppendLine(sb.ToString());
			}
			sb = null;
			return csvRows.ToString();
		}

		#endregion

		#region Send Email to Users

		private static Regex objRegExPassword = new Regex(
												  "\\[User\\:Password\\]",
												RegexOptions.IgnoreCase
												| RegexOptions.Multiline
												| RegexOptions.Singleline
												| RegexOptions.CultureInvariant
												| RegexOptions.Compiled
												);

		public static string SendEmail(UserInfo objUser, string SubjectTemplate, string BodyTemplate, PortalSettings objPortalSettings)
		{
			try
			{
				if (!IsValidEmail(objUser.Email))
				{
					return "InvalidEmail";
				}

				//send email
				DotNetNuke.Services.Tokens.TokenReplace objTokenReplace =
					new DotNetNuke.Services.Tokens.TokenReplace(
						DotNetNuke.Services.Tokens.Scope.DefaultSettings,
						System.Globalization.CultureInfo.CurrentCulture.Name,
						objPortalSettings,
						objUser);

				string Subject = objRegExPassword.Replace(SubjectTemplate, objUser.Membership.Password);
				string Body = objRegExPassword.Replace(BodyTemplate, objUser.Membership.Password);

				Subject = objTokenReplace.ReplaceEnvironmentTokens(Subject);
				Body = objTokenReplace.ReplaceEnvironmentTokens(Body);

				DotNetNuke.Services.Mail.Mail.SendEmail(objPortalSettings.Email, objUser.Email, Subject, Body);
			}
			catch (Exception Exc)
			{
				return Exc.Message;
			}
			return "";
		}

		public static bool IsValidEmail(string Email)
		{
			//check is empty
			if (Email.Trim() == "")
			{
				return false;
			}

			//check is valid at all
			try
			{
				MailAddress m = new MailAddress(Email);

				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}

		#endregion

		public static void ResponseFile(string ContentType, byte[] lstBytes, string FileName)
		{
			HttpResponse objResponse = System.Web.HttpContext.Current.Response;
			objResponse.ClearHeaders();
			objResponse.ClearContent();
			objResponse.ContentType = ContentType;//"application/vnd.ms-excel"
			objResponse.AppendHeader("Content-Length", lstBytes.Length.ToString());
			objResponse.AppendHeader("Content-Disposition", string.Format("attachment; filename={0}", FileName));
			objResponse.OutputStream.Write(lstBytes, 0, lstBytes.Length);
			objResponse.Flush();
			objResponse.End();
		}

		public static string SaveFile(int PortalId, string ToWrite, string FileName)
		{
			PortalSettings objPortalSettings = new PortalSettings(PortalId);
			string Path = objPortalSettings.HomeDirectoryMapPath + "UsersExportImport\\";
			if (!System.IO.Directory.Exists(Path))
			{
				System.IO.Directory.CreateDirectory(Path);
			}
			//delete all files in directory for security reason
			foreach (string tempName in System.IO.Directory.GetFiles(Path))
			{
				System.IO.File.Delete(tempName);
			}

			System.IO.StreamWriter sw = new System.IO.StreamWriter(Path + FileName, false, Encoding.UTF8);
			sw.Write(ToWrite);
			sw.Close();

			return objPortalSettings.HomeDirectory + "UsersExportImport/" + FileName;
		}

		public static string GetMaxAllowedFileSize()
		{
			try
			{
				System.Configuration.Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
				HttpRuntimeSection section = config.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
				return string.Format("{0} kB", section.MaxRequestLength);
			}
			catch
			{
				return "undefined";
			}
		}
	}
}