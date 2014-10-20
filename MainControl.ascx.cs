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
				dt.Columns.Add(Header);
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
					objUser.DisplayName = string.Format("{0}", dr["DisplayName"]);
					objUser.PortalID = this.PortalId;

					objUser.IsSuperUser = (string.Format("{0}", dr["IsSuperUser"]) == "1");
					objUser.Username = string.Format("{0}", dr["Username"]);

					if (dt.Columns.Contains("Password"))
					{
						objUser.Membership.Password = string.Format("{0}", dr["Password"]);
					}
					objUser.Membership.Password = ValidatePassword(objUser, cbRandomPassword.Checked);

					int AffiliateID = -1;
					if (Int32.TryParse(string.Format("{0}", dr["AffiliateId"]), out AffiliateID))
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
						objUser.Profile.SetProfileProperty("OldUserID", string.Format("{0}", dr["UserID"]));
						SuccessUsersCount++;

						//Update Profile
						objUser.Profile.Country = string.Format("{0}", dr["Country"]);
						objUser.Profile.Street = string.Format("{0}", dr["Street"]);
						objUser.Profile.City = string.Format("{0}", dr["City"]);
						objUser.Profile.Region = string.Format("{0}", dr["Region"]);
						objUser.Profile.PostalCode = string.Format("{0}", dr["PostalCode"]);
						objUser.Profile.Unit = string.Format("{0}", dr["Unit"]);
						objUser.Profile.Telephone = string.Format("{0}", dr["Telephone"]);
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

		#region Export

		protected void btnExportUsers_Click(object sender, EventArgs e)
		{
			DoExport();
		}

		private void DoExport()
		{
			IDataReader idr = null;

			//check if IsDeleted column exists
			bool IsDeletedExists = true;
			try
			{
				idr = DotNetNuke.Data.DataProvider.Instance().ExecuteSQL("SELECT	TOP 1 IsDeleted FROM {databaseOwner}{objectQualifier}vw_Users");
			}
			catch
			{
				IsDeletedExists = false;
			}

			//build dynamic query to retireve data
			Hashtable htFieldNames = new Hashtable();
			htFieldNames["UserID"] = 1;
			htFieldNames["UserName"] = 1;
			htFieldNames["FirstName"] = 1;
			htFieldNames["LastName"] = 1;
			htFieldNames["DisplayName"] = 1;
			htFieldNames["IsSuperUser"] = 1;
			htFieldNames["Email"] = 1;
			htFieldNames["AffiliateId"] = 1;
			htFieldNames["Authorised"] = 1;

			StringBuilder sbSelect = new StringBuilder(
@"SELECT	u.UserID,
		u.UserName,
		u.FirstName,
		u.LastName,
		u.DisplayName,
		u.IsSuperUser,
		u.Email,
		u.AffiliateId,
		u.Authorised");

			StringBuilder sbFrom = new StringBuilder(@"
 FROM	vw_Users u ");
			StringBuilder sbWhere = new StringBuilder(@"
 WHERE	(1=1) 
	AND (u.UserID IN (SELECT UserID FROM {databaseOwner}{objectQualifier}UserPortals up WHERE (((u.UserID=up.UserId) AND (up.PortalId={0})) OR (u.IsSuperUser=1)))) ".Replace("{0}", this.PortalId.ToString()));

			//check SuperUsers
			if (!cbIncludeSuperUsers.Checked)
			{
				sbWhere.Append(" AND (u.IsSuperUser=0) ");
			}

			//check deleted accounts
			if (IsDeletedExists)
			{
				sbSelect.Append(", u.IsDeleted");
				if (!cbIncludeDeleted.Checked)
				{
					sbWhere.Append(" AND (u.IsDeleted=0) ");
				}
				htFieldNames["IsDeleted"] = 1;
			}

			//check authorised accounts
			if (!cbIncludeNonAuthorised.Checked)
			{
				sbWhere.Append(" AND (u.Authorised=1) ");
			}

			//check if requires to export roles
			if (cbExportRoles.Checked)
			{
				sbSelect.Append(@",		(SELECT	CAST(ur.RoleID AS nvarchar(10)) + ','
		FROM	{databaseOwner}{objectQualifier}UserRoles ur
		WHERE	ur.UserID=u.UserID
		FOR XML PATH('')) RoleIDs,
		
		(SELECT	r.RoleName + ','
		FROM	{databaseOwner}{objectQualifier}UserRoles ur
				LEFT JOIN {databaseOwner}{objectQualifier}Roles r ON (ur.RoleID=r.RoleID)
		WHERE	ur.UserID=u.UserID
		FOR XML PATH('')) Roles 
");

				htFieldNames["RoleIDs"] = 1;
				htFieldNames["Roles"] = 1;
			}

			//define properties
			foreach (ListItem li in cblPropertiesToExport.Items)
			{
				if ((!li.Selected) || (htFieldNames[li.Text] != null))
				{
					continue;
				}
				htFieldNames[li.Text] = 1;

				sbSelect.Append(", up{0}.PropertyValue [{1}] "
					.Replace("{0}", li.Value)
					.Replace("{1}", li.Text)
				);


				sbFrom.Append(" LEFT JOIN {databaseOwner}{objectQualifier}UserProfile up{0} ON ((u.UserID=up{0}.UserID) AND (up{0}.PropertyDefinitionID={0})) "
					.Replace("{0}", li.Value));
			}

			idr = DotNetNuke.Data.DataProvider.Instance().ExecuteSQL(sbSelect.ToString() + sbFrom.ToString() + sbWhere.ToString());
			DataTable dt = DotNetNuke.Common.Globals.ConvertDataReaderToDataTable(idr);
			if (cbExportPasswords.Checked)
			{
				dt = AddPasswordsColumn(dt);
			}

			string strResult = "";

			switch (ddlExportFileType.SelectedValue)
			{
				case "0"://temporary disabled for Excel
					break;
				case "1"://XML - FOR XML RAW
					strResult = CommonController.ToXML(dt);
					CommonController.ResponseFile("text/xml", System.Text.Encoding.UTF8.GetBytes(strResult), string.Format("Users_{0:ddMMyyyy_HHmmss}.xml", DateTime.Now));
					break;
				case "2"://CSV
					strResult = CommonController.ToCSV(dt, true, ",");
					CommonController.ResponseFile("text/csv", System.Text.Encoding.UTF8.GetBytes(strResult), string.Format("Users_{0:ddMMyyyy_HHmmss}.csv", DateTime.Now));
					break;
			}

		}

		private DataTable AddPasswordsColumn(DataTable dt)
		{
			dt.Columns.Add("Password", "".GetType());

			foreach (DataRow dr in dt.Rows)
			{
				if (DotNetNuke.Security.Membership.MembershipProviderConfig.PasswordRetrievalEnabled)
				{
					UserInfo objUser = UserController.GetUserById(this.PortalId, (int)dr["userid"]);
					dr["Password"] = UserController.GetPassword(ref objUser, objUser.Membership.PasswordAnswer);
				}
				else
				{
					dr["Password"] = "";
				}
			}

			return dt;
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