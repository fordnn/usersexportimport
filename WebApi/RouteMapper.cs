using DotNetNuke.Web.Api;

namespace forDNN.UsersExportImport.WebApi
{
	/// <summary>
	/// Class Routemapper.
	/// </summary>
	public class Routemapper : IServiceRouteMapper
	{
		/// <summary>
		/// Registers the routes.
		/// </summary>
		/// <param name="routeManager">The route manager.</param>
		public void RegisterRoutes(IMapRoute routeManager)
		{
			routeManager.MapHttpRoute("forDNN.UsersExportImport", "default", "{controller}/{action}",
					new[] { "forDNN.Modules.UsersExportImport.WebApi" });
		}
	}
}


