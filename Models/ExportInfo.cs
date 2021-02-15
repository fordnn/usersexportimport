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

namespace forDNN.Modules.UsersExportImport.Models
{
	public class ExportInfo
	{
		public int ExportFileType { get; set; }
		public bool IncludeSuperUsers { get; set; }
		public bool IncludeDeleted { get; set; }
		public bool IncludeNonAuthorised { get; set; }
		public bool ExportRoles { get; set; }
		public bool ExportPasswords { get; set; }
		public string PropertiesToExport { get; set; }
		public int ExportByRole { get; set; }
	}
}