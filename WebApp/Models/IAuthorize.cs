using System.Web.Mvc;
using DBHelper;

namespace WebApp.Models
{
    public class AuthController : Controller
    {
        public IDbHelper Db;

        public AuthController(IDbHelper db) => Db = db;
    }
}