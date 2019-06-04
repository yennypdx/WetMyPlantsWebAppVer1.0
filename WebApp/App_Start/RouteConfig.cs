using System.Web.Mvc;
using System.Web.Routing;

namespace WebApp
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes(); // Enable attribute routing

            routes.MapRoute(
                name: "Home",
                url: "",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                }
            );
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );
            routes.MapRoute(
              name: "ResetPassword",
              url: "{controller}/{action}/{userId}",
              defaults: new
              {
                  controller = "Account",
                  action = "ResetUserPassword",
                  userId = UrlParameter.Optional
              }
            );
        }
    }
}
