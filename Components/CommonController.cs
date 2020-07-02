using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security.Roles;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Localization;
using DotNetNuke.Entities.Profile;

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

		#region Import functionality

		private static string RemoveWhiteSpaces(string Source)
		{
			//remove Zero width space and Zero width no-break space
			return Source.Trim(new char[] { '\uFEFF', '\u200B' });
		}

		private static DataTable CSV2Table(byte[] lstBytes)
		{
			string Separator = ",";

			DataTable dt = new DataTable();

			string strTable = System.Text.Encoding.UTF8.GetString(lstBytes);
			string[] lstRows = strTable.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			if (lstRows.Length == 0)
			{
				return dt;
			}

			foreach (string Header in lstRows[0].Split(new char[] { ',' }))
			{
				//lets remove white space chars from the column's name
				dt.Columns.Add(RemoveWhiteSpaces(Header));
			}

			for (int i = 1; (i < lstRows.Length - 1); i++)
			{
				ArrayList lstValues = new ArrayList();
				string Value = lstRows[i];

				bool WithinQuotes = false;
				int k = 0;
				StringBuilder SubValue = new StringBuilder();
				while (k < Value.Length)
				{
					if (Value[k].ToString() == Separator)
					{
						//check is it new column
						if (!WithinQuotes)
						{
							//yes, its new column
							lstValues.Add(SubValue.ToString());
							SubValue = new StringBuilder();
							k++;
							continue;
						}
					}

					if (Value[k].ToString() == "\"")
					{
						//check is it begin/end of new string column or just quote char
						if (k < Value.Length - 1)
						{
							if ((Value[k + 1].ToString() == "\""))
							{
								//this is quote char, replace it with single quote and move cursor next to it
								SubValue.Append(Value[k]);
								k++;
							}
							else
							{
								//this is begin/end of new string column - skip quote char and reverse WithinQuotes flag
								WithinQuotes = !WithinQuotes;
							}
						}
					}
					else
					{
						SubValue.Append(Value[k]);
					}

					k++;
				}

				if (SubValue.ToString() != "")
				{
					lstValues.Add(SubValue.ToString());
				}

				//add new row to DataTable
				DataRow dr = dt.NewRow();
				for (k = 0; k < dt.Columns.Count; k++)
				{
					try
					{
						dr[k] = lstValues[k];
					}
					catch { }
				}
				dt.Rows.Add(dr);
			}

			return dt;
		}

		private static DataTable XML2Table(byte[] lstBytes)
		{
			string strXML = System.Text.Encoding.UTF8.GetString(lstBytes);
			System.Xml.XmlDocument objDoc = new System.Xml.XmlDocument();
			objDoc.LoadXml(strXML);

			DataTable dt = new DataTable();
			bool RequireColumns = true;
			foreach (System.Xml.XmlNode xmlUser in objDoc.DocumentElement.ChildNodes)
			{
				if (RequireColumns)
				{
					foreach (System.Xml.XmlAttribute xmlAttr in xmlUser.Attributes)
					{
						dt.Columns.Add(xmlAttr.Name);
					}
					RequireColumns = false;
				}

				DataRow dr = dt.NewRow();
				foreach (System.Xml.XmlAttribute xmlAttr in xmlUser.Attributes)
				{
					dr[xmlAttr.Name] = xmlAttr.Value;
				}
				dt.Rows.Add(dr);
			}

			return dt;
		}

		private static string ValidatePassword(UserInfo objUser, bool RandomPassword)
		{
			if (!RandomPassword)
			{
				objUser.Membership.Password = (objUser.Membership.Password == null) ? "" : objUser.Membership.Password;
				if (UserController.ValidatePassword(objUser.Membership.Password))
				{
					return objUser.Membership.Password;
				}
			}
			return UserController.GeneratePassword();
		}

		private static object GetDataRowValue(DataTable dt, DataRow dr, string FieldName, object DefaultValue)
		{
			if (dt.Columns.IndexOf(FieldName) == -1)
			{
				return DefaultValue;
			}
			return dr[FieldName];
		}

		private static bool ObjectToBool(object Src)
		{
			string temp = string.Format("{0}", Src).ToLowerInvariant();
			return (temp == "1") || (temp == "true");
		}

		public static string DoImport(PortalSettings objPortalSettings, string LocalResourceFile, UserInfo currentUserInfo)
		{
			System.Web.HttpContext objContext = System.Web.HttpContext.Current;

			if (objContext.Request.Files.Count == 0)
			{
				return Localization.GetString("ImportFileMissed", LocalResourceFile);
			}

			HttpPostedFile objFile = objContext.Request.Files[0];

			System.IO.FileInfo objFileInfo = new System.IO.FileInfo(objFile.FileName);
			byte[] lstBytes = new byte[(int)objFile.InputStream.Length];
			objFile.InputStream.Read(lstBytes, 0, (int)objFile.InputStream.Length);

			DataTable dt = null;

			switch (objFileInfo.Extension.ToLower())
			{
				case ".csv":
				case ".txt":
					dt = CSV2Table(lstBytes);
					break;
				case ".xml":
					dt = XML2Table(lstBytes);
					break;
			}

			ProfilePropertyDefinition objOldUserID = DotNetNuke.Entities.Profile.ProfileController.GetPropertyDefinitionByName(objPortalSettings.PortalId, "OldUserID");
			if (objOldUserID == null)
			{
				objOldUserID = new ProfilePropertyDefinition(objPortalSettings.PortalId);
				objOldUserID.PropertyName = "OldUserID";
				objOldUserID.PropertyCategory = "Imported";
				objOldUserID.DataType = 349;
				DotNetNuke.Entities.Profile.ProfileController.AddPropertyDefinition(objOldUserID);
			}

			string NonProfileFields = ",userid,username,firstname,lastname,displayname,issuperuser,email,affiliateid,authorised,isdeleted,roleids,roles,";

			string SubjectTemplate = Localization.GetString("EmailUserSubject", LocalResourceFile);
			string BodyTemplate = Localization.GetString("EmailUserBody", LocalResourceFile);

			int UsersCount = 0;
			int SuccessUsersCount = 0;
			StringBuilder FailedUsers = new StringBuilder();

			foreach (DataRow dr in dt.Rows)
			{
				UsersCount++;
				try
				{
					DotNetNuke.Entities.Users.UserInfo objUser = new DotNetNuke.Entities.Users.UserInfo();
					objUser.Profile.InitialiseProfile(objPortalSettings.PortalId, true);
					objUser.Email = string.Format("{0}", dr["Email"]);
					objUser.FirstName = string.Format("{0}", dr["FirstName"]);
					objUser.LastName = string.Format("{0}", dr["LastName"]);
					objUser.DisplayName = string.Format("{0}", GetDataRowValue(dt, dr, "DisplayName", string.Format("{0} {1}", dr["FirstName"], dr["LastName"])));
					objUser.PortalID = objPortalSettings.PortalId;

					objUser.IsSuperUser = ObjectToBool(GetDataRowValue(dt, dr, "IsSuperUser", "0"));

					//only SuperUsers allowed to import users with SuperUsers rights
					if ((!currentUserInfo.IsSuperUser) && objUser.IsSuperUser)
					{
						FailedUsers.AppendFormat(
							string.Format(Localization.GetString("Line", LocalResourceFile), UsersCount) +
							Localization.GetString("UserDeniedToImportRole", LocalResourceFile),
							currentUserInfo.Username,
							"SuperUser");
						continue;
					}

					//use email as username, when username is not provided
					objUser.Username = string.Format("{0}", GetDataRowValue(dt, dr, "Username", objUser.Email));

					if (dt.Columns.Contains("Password"))
					{
						objUser.Membership.Password = string.Format("{0}", dr["Password"]);
					}
					objUser.Membership.Password = ValidatePassword(objUser, Convert.ToBoolean(objContext.Request.Form["cbRandomPassword"]));

					int AffiliateID = -1;
					if (Int32.TryParse(string.Format("{0}", GetDataRowValue(dt, dr, "AffiliateId", -1)), out AffiliateID))
					{
						objUser.AffiliateID = AffiliateID;
					}

					objUser.Membership.Approved = true;
					objUser.Membership.PasswordQuestion = objUser.Membership.Password;
					objUser.Membership.UpdatePassword = Convert.ToBoolean(objContext.Request.Form["cbForcePasswordChange"]);//update password on next login

					DotNetNuke.Security.Membership.UserCreateStatus objCreateStatus =
						DotNetNuke.Entities.Users.UserController.CreateUser(ref objUser);
					if (objCreateStatus == DotNetNuke.Security.Membership.UserCreateStatus.Success)
					{
						if (dt.Columns.IndexOf("UserID") != -1)
						{
							objUser.Profile.SetProfileProperty("OldUserID", string.Format("{0}", dr["UserID"]));
						}
						SuccessUsersCount++;

						//Update Profile
						objUser.Profile.Country = string.Format("{0}", GetDataRowValue(dt, dr, "Country", ""));
						objUser.Profile.Street = string.Format("{0}", GetDataRowValue(dt, dr, "Street", ""));
						objUser.Profile.City = string.Format("{0}", GetDataRowValue(dt, dr, "City", ""));
						objUser.Profile.Region = string.Format("{0}", GetDataRowValue(dt, dr, "Region", ""));
						objUser.Profile.PostalCode = string.Format("{0}", GetDataRowValue(dt, dr, "PostalCode", ""));
						objUser.Profile.Unit = string.Format("{0}", GetDataRowValue(dt, dr, "Unit", ""));
						objUser.Profile.Telephone = string.Format("{0}", GetDataRowValue(dt, dr, "Telephone", ""));
						objUser.Profile.FirstName = objUser.FirstName;
						objUser.Profile.LastName = objUser.LastName;

						//Profile Properties
						if (Convert.ToBoolean(objContext.Request.Form["cbImportProfileProperties.Checked"]))
						{
							foreach (DataColumn dc in dt.Columns)
							{
								if (NonProfileFields.IndexOf(string.Format(",{0},", dc.ColumnName.ToLower())) != -1)
								{
									continue;
								}
								//check if profile property exists
								ProfilePropertyDefinition objPPD = DotNetNuke.Entities.Profile.ProfileController.GetPropertyDefinitionByName(objPortalSettings.PortalId, dc.ColumnName);
								if ((objPPD == null) && (Convert.ToBoolean(objContext.Request.Form["cbCreateMissedProfileProperties"])))
								{
									objPPD = new ProfilePropertyDefinition(objPortalSettings.PortalId);
									objPPD.PropertyName = dc.ColumnName;
									objPPD.PropertyCategory = "Imported";
									objPPD.DataType = 349;
									DotNetNuke.Entities.Profile.ProfileController.AddPropertyDefinition(objPPD);
									objUser.Profile.SetProfileProperty(dc.ColumnName, string.Format("{0}", dr[dc.ColumnName]));
								}
								else
								{
									objUser.Profile.SetProfileProperty(dc.ColumnName, string.Format("{0}", dr[dc.ColumnName]));
								}
							}
						}
						ProfileController.UpdateUserProfile(objUser);
						UserController.UpdateUser(objPortalSettings.PortalId, objUser);

						//Update Roles
						string RolesStatus = UpdateRoles(currentUserInfo, objPortalSettings, objUser, dr, objContext.Request.Form["rblImportRoles"], LocalResourceFile);
						if (RolesStatus.Trim() != "")
						{
							FailedUsers.AppendFormat(Localization.GetString("UpdateRolesError", LocalResourceFile),
								UsersCount,
								objUser.UserID,
								RolesStatus);
						}

						if (Convert.ToBoolean(objContext.Request.Form["cbEmailUser"]))
						{
							string SendEmailResult = CommonController.SendEmail(objUser, SubjectTemplate, BodyTemplate, objPortalSettings);

							switch (SendEmailResult)
							{
								case "":
									//success
									break;
								case "InvalidEmail":
									FailedUsers.AppendFormat(Localization.GetString("SendEmailInvalidEmailException", LocalResourceFile),
										UsersCount,
										objUser.Username,
										objUser.Email);
									break;
								default:
									FailedUsers.AppendFormat(Localization.GetString("SendEmailException", LocalResourceFile),
										UsersCount,
										objUser.Username,
										SendEmailResult);
									break;
							}
						}
					}
					else
					{
						FailedUsers.AppendFormat(Localization.GetString("RowMembershipError", LocalResourceFile),
							UsersCount,
							objUser.Username,
							objCreateStatus.ToString());
					}
				}
				catch (Exception Exc)
				{
					FailedUsers.AppendFormat(Localization.GetString("RowException", LocalResourceFile),
						UsersCount,
						Exc.Message);
				}
			}
			return string.Format(Localization.GetString("Result", LocalResourceFile),
				UsersCount,
				SuccessUsersCount,
				FailedUsers.ToString());
		}

		private static string UpdateRoles(UserInfo objCurrentUser,
			PortalSettings objPortalSettings,
			UserInfo objUser,
			DataRow dr,
			string rblImportRoles,
			string LocalResourceFile
			)
		{
			RoleController objRoleController = new RoleController();
			bool ByID = false;
			bool AutoCreateRole = false;
			string Roles = "";

			switch (rblImportRoles)
			{
				case "0":
					return "";
				case "1":
					ByID = true;
					Roles = (string)dr["RoleIDs"];
					break;
				case "3":
					Roles = (string)dr["Roles"];
					AutoCreateRole = true;
					break;
				case "2":
					Roles = (string)dr["Roles"];
					break;
			}

			StringBuilder sb = new StringBuilder();
			foreach (string Role in Roles.Split(new char[] { ',' }))
			{
				if (Role.Trim() == "")
				{
					continue;
				}

				RoleInfo objRole = null;
				if (ByID)
				{
					int RoleID = -1;
					if (Int32.TryParse(Role, out RoleID))
					{
						objRole = objRoleController.GetRole(RoleID, objPortalSettings.PortalId);
					}
				}
				else
				{
					objRole = objRoleController.GetRoleByName(objPortalSettings.PortalId, Role);
					if ((objRole == null) && (AutoCreateRole))
					{
						objRole = new RoleInfo();
						objRole.RoleGroupID = -1;
						objRole.RoleName = Role;
						objRole.PortalID = objPortalSettings.PortalId;
						objRole.AutoAssignment = false;
						objRole.IsPublic = false;
						objRole.TrialPeriod = -1;
						objRole.BillingPeriod = -1;
						objRole.Status = RoleStatus.Approved;
						objRole.RoleID = objRoleController.AddRole(objRole);
					}
				}

				//check current user has permissions to import user with specific role
				if (!
					(objCurrentUser.IsInRole(objRole.RoleName) ||
					objCurrentUser.IsInRole(objPortalSettings.AdministratorRoleName) ||
					objCurrentUser.IsSuperUser
					))
				{
					sb.AppendFormat(Localization.GetString("UserDeniedToImportRole", LocalResourceFile), objCurrentUser.Username,
						(ByID ? string.Format("RoleID={0}", objRole.RoleID) : string.Format("RoleName={0}", objRole.RoleName))
					);
					continue;
				}

				if (objRole != null)
				{
					objRoleController.AddUserRole(objPortalSettings.PortalId, objUser.UserID, objRole.RoleID, DotNetNuke.Common.Utilities.Null.NullDate);
				}
				else
				{
					sb.AppendFormat(
						ByID ? Localization.GetString("RoleIDNotFound", LocalResourceFile) : Localization.GetString("RoleNameNotFound", LocalResourceFile),
						Role);
				}
			}
			return sb.ToString();
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

		public static string ResolveUrl(string FileName, bool VirtualPathOnly)
		{
			System.Web.HttpRequest objRequest = System.Web.HttpContext.Current.Request;
			string Secure = "";
			string Port = "";

			if (objRequest.IsSecureConnection)
			{
				Secure = "s";
			}
			if (!objRequest.Url.IsDefaultPort)
			{
				Port = ":" + objRequest.Url.Port.ToString();
			}

			string FullRoot = "";
			if (!VirtualPathOnly)
			{
				FullRoot = string.Format("http{0}://{1}{2}",
					Secure,
					objRequest.Url.Host,
					Port);
			}

			string Result = string.Format("{0}{1}DesktopModules/forDNN.UsersExportImport/{2}",
				FullRoot,
				(objRequest.ApplicationPath == "") ? "" : string.Format("{0}/", objRequest.ApplicationPath),
				FileName);

			return Result;
		}

	}
}