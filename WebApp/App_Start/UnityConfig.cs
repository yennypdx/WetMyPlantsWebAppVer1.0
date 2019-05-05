using DbHelper;
using System.Web.Mvc;
using Unity;
using Unity.Mvc5;

namespace WebApp
{
	public static class UnityConfig
	{
		public static void RegisterComponents()
		{
			var container = new UnityContainer();

			// register all your components with the container here
			// it is NOT necessary to register your controllers

			// e.g. container.RegisterType<ITestService, TestService>();
			container.RegisterType<IDbHelper, DbHelper.DbHelper>();
			// Must have RegisterInstance in our case because the constructor takes a parameter
			container.RegisterInstance<DbHelper.DbHelper>(new DbHelper.DbHelper(AccessHelper.GetDbConnectionString()));
			DependencyResolver.SetResolver(new UnityDependencyResolver(container));
		}
	}
}