using DotNetNuke.Web.Api;
using DotNetNuke.Security;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace forDNN.Modules.UsersExportImport.WebApi
{
	[SupportedModules("forDNN.UsersExportImport")]
	public class ExportImportController : DnnApiController
	{
		[HttpPost]
		[ActionName("DoExport")]
		[ValidateAntiForgeryToken]
		[DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
		public HttpResponseMessage DoExport([FromBody]Models.ExportInfo objValues)
		{
			try
			{
				return Request.CreateResponse(HttpStatusCode.OK, Controller.ExportController.DoExport(this.PortalSettings.PortalId, objValues));
			}
			catch (System.Exception ex)
			{
				DotNetNuke.Services.Exceptions.Exceptions.LogException(ex);
				return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
			}
		}

		[HttpPost]
		[ActionName("DoImport")]
		[ValidateAntiForgeryToken]
		[DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
		public HttpResponseMessage DoImport()
		{
			try
			{
				string LocalResourceFile = CommonController.ResolveUrl("App_LocalResources/MainControl", true);
				string Result = CommonController.DoImport(this.PortalSettings, LocalResourceFile, this.UserInfo);
				return Request.CreateResponse(HttpStatusCode.OK, Result);
			}
			catch (System.Exception ex)
			{
				DotNetNuke.Services.Exceptions.Exceptions.LogException(ex);
				return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
			}
		}

    }
}