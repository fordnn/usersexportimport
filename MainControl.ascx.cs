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
			btnImport.Attributes.Add("onclick", string.Format("javascript:return doImport({0});", this.ModuleId));
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