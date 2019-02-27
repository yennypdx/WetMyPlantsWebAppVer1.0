using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DBHelper;

namespace WebApp.Controllers
{
    public class APIController : ApiController
    {
        private readonly IDbHelper _db;

        public APIController(IDbHelper db) => _db = db; // DbHelper injected through constructor.
    }
}
