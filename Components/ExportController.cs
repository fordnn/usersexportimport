using System;
using System.Data;
using System.Collections;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using DotNetNuke.Entities.Users;
using DotNetNuke.Entities.Portals;

namespace forDNN.Modules.UsersExportImport.Controller
{
	public class ExportController
	{
		#region Export

		public static string DoExport(int PortalId, Models.ExportInfo objExportInfo)
		{
			string ResultURL = "";
			try
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
 FROM	 {databaseOwner}{objectQualifier}vw_Users u ");
				StringBuilder sbWhere = new StringBuilder(@"
 WHERE	(1=1) 
	AND ((u.PortalId={0}) OR (u.IsSuperUser=1)) ".Replace("{0}", PortalId.ToString()));

				//check SuperUsers
				if (!objExportInfo.IncludeSuperUsers)
				{
					sbWhere.Append(" AND (u.IsSuperUser=0) ");
				}

				//check deleted accounts
				if (IsDeletedExists)
				{
					sbSelect.Append(", u.IsDeleted");
					if (!objExportInfo.IncludeDeleted)
					{
						sbWhere.Append(" AND (u.IsDeleted=0) ");
					}
					htFieldNames["IsDeleted"] = 1;
				}

				//check authorised accounts
				if (!objExportInfo.IncludeNonAuthorised)
				{
					sbWhere.Append(" AND (u.Authorised=1) ");
				}

				//check if requires to export roles
				if (objExportInfo.ExportRoles)
				{
					sbSelect.Append(@",		(SELECT	CAST(ur.RoleID AS nvarchar(10)) + ','
		FROM	{databaseOwner}{objectQualifier}UserRoles ur
		WHERE	(ur.UserID=u.UserID) AND (ur.RoleID IN (SELECT r.RoleID FROM {databaseOwner}{objectQualifier}Roles r WHERE (r.PortalID={0}))) 
		FOR XML PATH('')) RoleIDs,
		
		(SELECT	r.RoleName + ','
		FROM	{databaseOwner}{objectQualifier}UserRoles ur
				LEFT JOIN {databaseOwner}{objectQualifier}Roles r ON (ur.RoleID=r.RoleID)
		WHERE	(ur.UserID=u.UserID) AND (ur.RoleID IN (SELECT r.RoleID FROM {databaseOwner}{objectQualifier}Roles r WHERE (r.PortalID={0})))
		FOR XML PATH('')) Roles 
").Replace("{0}", PortalId.ToString());

					htFieldNames["RoleIDs"] = 1;
					htFieldNames["Roles"] = 1;
				}

				//define properties
				foreach (string li in objExportInfo.PropertiesToExport.Split(new char[] { ',' }))
				{
					string[] objParam = li.Split(new char[] { '=' });
					if (htFieldNames[objParam[0]] != null)
					{
						continue;
					}

					htFieldNames[objParam[0]] = 1;

					sbSelect.Append(", up{0}.PropertyValue [{1}] "
						.Replace("{0}", objParam[1])
						.Replace("{1}", objParam[0])
					);


					sbFrom.Append(" LEFT JOIN {databaseOwner}{objectQualifier}UserProfile up{0} ON ((u.UserID=up{0}.UserID) AND (up{0}.PropertyDefinitionID={0})) "
						.Replace("{0}", objParam[1]));
				}

				idr = DotNetNuke.Data.DataProvider.Instance().ExecuteSQL(sbSelect.ToString() + sbFrom.ToString() + sbWhere.ToString());
				DataTable dt = DotNetNuke.Common.Globals.ConvertDataReaderToDataTable(idr);
				if (objExportInfo.ExportPasswords)
				{
					dt = AddPasswordsColumn(PortalId, dt);
				}

				string strResult = "";

				switch (objExportInfo.ExportFileType)
				{
					case 0://temporary disabled for Excel
						break;
					case 1://XML - FOR XML RAW
						strResult = CommonController.ToXML(dt);
						ResultURL = CommonController.SaveFile(PortalId,
							strResult,
							string.Format("Users_{0:ddMMyyyy_HHmmss}_{1}.xml", DateTime.Now, System.Guid.NewGuid().ToString()));
						break;
					case 2://CSV
						strResult = CommonController.ToCSV(dt, true, ",");
						ResultURL = CommonController.SaveFile(PortalId,
							strResult,
							string.Format("Users_{0:ddMMyyyy_HHmmss}_{1}.csv", DateTime.Now, System.Guid.NewGuid().ToString()));
						break;
				}
			}
			catch (System.Exception ex)
			{
				DotNetNuke.Services.Exceptions.Exceptions.LogException(ex);
			}

			return ResultURL;
		}

		private static DataTable AddPasswordsColumn(int PortalId, DataTable dt)
		{
			dt.Columns.Add("Password", "".GetType());

			foreach (DataRow dr in dt.Rows)
			{
				if (DotNetNuke.Security.Membership.MembershipProviderConfig.PasswordRetrievalEnabled)
				{
					UserInfo objUser = UserController.GetUserById(PortalId, (int)dr["userid"]);
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
	
	}
}