using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using DotNetNuke;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Security;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Profile;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.UI.UserControls;

using DotNetNuke.Security.Roles;


namespace forDNN.Modules.UsersExportImport
{
	partial class MainControl : PortalModuleBase, IActionable
	{

		#region "Event Handlers"

		private void FillProperties()
		{
			cblPropertiesToExport.DataSource = ProfileController.GetPropertyDefinitionsByPortal(this.PortalId);
			cblPropertiesToExport.DataTextField = "PropertyName";
			cblPropertiesToExport.DataValueField = "PropertyDefinitionId";
			cblPropertiesToExport.DataBind();
		}

		private void ExtraPageLoad()
		{
			lblIcon.Visible = true;
			lblIcon.Style["display"] = "block";
			lblIcon.Text = "<a href=\"http://forDNN.com\" target=\"_blank\"><img src=\"http://forDNN.com/forDNNTeam.gif\" border=\"0\"/></a>";

			lnkExampleCSV.NavigateUrl = ResolveUrl("Examples/Users_ExpotImport_CSV_Example.csv");
			lnkExampleXML.NavigateUrl = ResolveUrl("Examples/Users_ExpotImport_XML_Example.xml");

			cbImportProfileProperties.Attributes.Add("onchange",
				string.Format("javascript:importProfileProperties('#{0}', '#{1}');", cbImportProfileProperties.ClientID, cbCreateMissedProfileProperties.ClientID));

			lblMaxAllowedFileSize.Text = string.Format(Localization.GetString("MaxAllowedFileSize", this.LocalResourceFile), CommonController.GetMaxAllowedFileSize());

			if (this.UserInfo != null)
			{
				divIncludeSuperUsers.Visible = this.UserInfo.IsSuperUser;
			}

			if (!IsPostBack)
			{
				FillProperties();

				cbPropertiesToExport.Attributes.Add("onchange",
					string.Format("javascript:$('#divPropertiesToExport input').prop('checked', $('#{0}').is(':checked'));", cbPropertiesToExport.ClientID));

				if (!DotNetNuke.Security.Membership.MembershipProviderConfig.PasswordRetrievalEnabled)
				{
					cbExportPasswords.Enabled = false;
					lblExportPasswordsDisabled.Visible = true;
				}
			}

			btnExportUsers.Attributes.Add("onclick", string.Format("javascript:return doExport({0});", this.ModuleId));
		}

		protected void Page_Load(object sender, System.EventArgs e)
		{
			try
			{
				ExtraPageLoad();
			}

			catch (Exception exc)
			{
				//Module failed to load 
				Exceptions.ProcessModuleLoadException(this, exc);
			}
		}

		#endregion

		#region Import

		private string RemoveWhiteSpaces(string Source)
		{
			//remove Zero width space and Zero width no-break space
			return Source.Trim(new char[] { '\uFEFF', '\u200B' });
		}

		private DataTable CSV2Table(byte[] lstBytes)
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

		private DataTable XML2Table(byte[] lstBytes)
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

		private string ValidatePassword(UserInfo objUser, bool RandomPassword)
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

		private object GetDataRowValue(DataTable dt, DataRow dr, string FieldName, object DefaultValue)
		{
			if (dt.Columns.IndexOf(FieldName) == -1)
			{
				return DefaultValue;
			}
			return dr[FieldName];
		}

		private void DoImport()
		{
			if (objFile.PostedFile.FileName == "")
			{
				DotNetNuke.UI.Skins.Skin.AddModuleMessage(
					this,
					Localization.GetString("ImportFileMissed", this.LocalResourceFile),
					DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.RedError);
				return;
			}

			System.IO.FileInfo objFileInfo = new System.IO.FileInfo(objFile.PostedFile.FileName);
			byte[] lstBytes = new byte[(int)objFile.PostedFile.InputStream.Length];
			objFile.PostedFile.InputStream.Read(lstBytes, 0, (int)objFile.PostedFile.InputStream.Length);

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

			ProfilePropertyDefinition objOldUserID = DotNetNuke.Entities.Profile.ProfileController.GetPropertyDefinitionByName(this.PortalId, "OldUserID");
			if (objOldUserID == null)
			{
				objOldUserID = new ProfilePropertyDefinition(this.PortalId);
				objOldUserID.PropertyName = "OldUserID";
				objOldUserID.PropertyCategory = "Imported";
				objOldUserID.DataType = 349;
				DotNetNuke.Entities.Profile.ProfileController.AddPropertyDefinition(objOldUserID);
			}

			string NonProfileFields = ",userid,username,firstname,lastname,displayname,issuperuser,email,affiliateid,authorised,isdeleted,roleids,roles,";

			string SubjectTemplate = Localization.GetString("EmailUserSubject", this.LocalResourceFile);
			string BodyTemplate = Localization.GetString("EmailUserBody", this.LocalResourceFile);

			int UsersCount = 0;
			int SuccessUsersCount = 0;
			StringBuilder FailedUsers = new StringBuilder();

			foreach (DataRow dr in dt.Rows)
			{
				UsersCount++;
				try
				{
					DotNetNuke.Entities.Users.UserInfo objUser = new DotNetNuke.Entities.Users.UserInfo();
					objUser.Profile.InitialiseProfile(this.PortalId, true);
					objUser.Email = string.Format("{0}", dr["Email"]);
					objUser.FirstName = string.Format("{0}", dr["FirstName"]);
					objUser.LastName = string.Format("{0}", dr["LastName"]);
					objUser.DisplayName = string.Format("{0}", GetDataRowValue(dt, dr, "DisplayName", string.Format("{0} {1}", dr["FirstName"], dr["LastName"]) ));
					objUser.PortalID = this.PortalId;

					objUser.IsSuperUser = (string.Format("{0}", GetDataRowValue(dt, dr, "IsSuperUser", "0")) == "1");

					//use email as username, when username is not provided
					objUser.Username = string.Format("{0}", GetDataRowValue(dt, dr, "Username", objUser.Email));

					if (dt.Columns.Contains("Password"))
					{
						objUser.Membership.Password = string.Format("{0}", dr["Password"]);
					}
					objUser.Membership.Password = ValidatePassword(objUser, cbRandomPassword.Checked);

					int AffiliateID = -1;
					if (Int32.TryParse(string.Format("{0}", GetDataRowValue(dt, dr, "AffiliateId", -1)), out AffiliateID))
					{
						objUser.AffiliateID = AffiliateID;
					}

					objUser.Membership.Approved = true;
					objUser.Membership.PasswordQuestion = objUser.Membership.Password;
					objUser.Membership.UpdatePassword = cbForcePasswordChange.Checked;//update password on next login

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
						objUser.Profile.Country =	string.Format("{0}", GetDataRowValue(dt, dr, "Country", ""));
						objUser.Profile.Street =	string.Format("{0}", GetDataRowValue(dt, dr, "Street", ""));
						objUser.Profile.City =		string.Format("{0}", GetDataRowValue(dt, dr, "City", ""));
						objUser.Profile.Region =	string.Format("{0}", GetDataRowValue(dt, dr, "Region", ""));
						objUser.Profile.PostalCode =	string.Format("{0}", GetDataRowValue(dt, dr, "PostalCode", ""));
						objUser.Profile.Unit =		string.Format("{0}", GetDataRowValue(dt, dr, "Unit", ""));
						objUser.Profile.Telephone =	string.Format("{0}", GetDataRowValue(dt, dr, "Telephone", ""));
						objUser.Profile.FirstName = objUser.FirstName;
						objUser.Profile.LastName = objUser.LastName;

						//Profile Properties
						if (cbImportProfileProperties.Checked)
						{
							foreach (DataColumn dc in dt.Columns)
							{
								if (NonProfileFields.IndexOf(string.Format(",{0},", dc.ColumnName.ToLower())) != -1)
								{
									continue;
								}
								//check if profile property exists
								ProfilePropertyDefinition objPPD = DotNetNuke.Entities.Profile.ProfileController.GetPropertyDefinitionByName(this.PortalId, dc.ColumnName);
								if ((objPPD == null) && (cbCreateMissedProfileProperties.Checked))
								{
									objPPD = new ProfilePropertyDefinition(this.PortalId);
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
						UserController.UpdateUser(this.PortalId, objUser);

						//Update Roles
						string RolesStatus = UpdateRoles(objUser, dr);
						if (RolesStatus.Trim() != "")
						{
							FailedUsers.AppendFormat(Localization.GetString("UpdateRolesError", this.LocalResourceFile),
								UsersCount,
								objUser.UserID,
								RolesStatus);
						}

						if (cbEmailUser.Checked)
						{
							string SendEmailResult = CommonController.SendEmail(objUser, SubjectTemplate, BodyTemplate, this.PortalSettings);

							switch (SendEmailResult)
							{
								case "":
									//success
									break;
								case "InvalidEmail":
									FailedUsers.AppendFormat(Localization.GetString("SendEmailInvalidEmailException", this.LocalResourceFile),
										UsersCount,
										objUser.Username,
										objUser.Email);
									break;
								default:
									FailedUsers.AppendFormat(Localization.GetString("SendEmailException", this.LocalResourceFile),
										UsersCount,
										objUser.Username,
										SendEmailResult);
									break;
							}
						}
					}
					else
					{
						FailedUsers.AppendFormat(Localization.GetString("RowMembershipError", this.LocalResourceFile),
							UsersCount,
							objUser.Username,
							objCreateStatus.ToString());
					}
				}
				catch (Exception Exc)
				{
					FailedUsers.AppendFormat(Localization.GetString("RowException", this.LocalResourceFile),
						UsersCount,
						Exc.Message);
				}
			}
			lblResult.Text = string.Format(Localization.GetString("Result", this.LocalResourceFile),
				UsersCount,
				SuccessUsersCount,
				FailedUsers.ToString());
		}

		private string UpdateRoles(UserInfo objUser, DataRow dr)
		{
			RoleController objRoleController = new RoleController();
			bool ByID = false;
			string Roles = "";

			switch (rblImportRoles.SelectedValue)
			{
				case "0":
					return "";
				case "1":
					ByID = true;
					Roles = (string)dr["RoleIDs"];
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
						objRole = objRoleController.GetRole(RoleID, this.PortalId);
					}
				}
				else
				{
					objRole = objRoleController.GetRoleByName(this.PortalId, Role);
				}

				if (objRole != null)
				{
					objRoleController.AddUserRole(this.PortalId, objUser.UserID, objRole.RoleID, Null.NullDate);
				}
				else
				{
					sb.AppendFormat(
						ByID ? Localization.GetString("RoleIDNotFound", this.LocalResourceFile) : Localization.GetString("RoleNameNotFound", this.LocalResourceFile),
						Role);
				}
			}
			return sb.ToString();
		}

		protected void btnImport_Click(object sender, EventArgs e)
		{
			DoImport();
		}

		#endregion

		#region "Optional Interfaces"

		/// ----------------------------------------------------------------------------- 
		/// <summary> 
		/// Registers the module actions required for interfacing with the portal framework 
		/// </summary> 
		/// <value></value> 
		/// <returns></returns> 
		/// <remarks></remarks> 
		/// <history> 
		/// </history> 
		/// ----------------------------------------------------------------------------- 
		public ModuleActionCollection ModuleActions
		{
			get
			{
				ModuleActionCollection Actions = new ModuleActionCollection();
				//Actions.Add(GetNextActionID(), Localization.GetString(ModuleActionType.AddContent, this.LocalResourceFile),
				//   ModuleActionType.AddContent, "", "add.gif", EditUrl(), false, DotNetNuke.Security.SecurityAccessLevel.Edit,
				//    true, false);
				return Actions;
			}
		}

		#endregion

	}

}