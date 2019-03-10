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
			container.RegisterType<DBHelper.IDbHelper, DBHelper.DbHelper>();
			// Must have RegisterInstance in our case because the constructor takes a parameter
			container.RegisterInstance<DBHelper.DbHelper>(new DBHelper.DbHelper(DBHelper.AccessHelper.GetDbConnectionString()));
			DependencyResolver.SetResolver(new UnityDependencyResolver(container));
		}
	}
}