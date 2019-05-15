using System;
using System.Net;
using DbHelper;
using Models;
using System.Web.Mvc;

namespace WebApp.Controllers
{
    [RoutePrefix("piapi")]
    public class PiAPIController : Controller
    {
        private readonly IDbHelper _db;

        private JsonResult Jsonify(string content) => Json($"{{ content: '{content}' }}");
        // BadRequest takes a string or JSON object and returns it along with a 500 (BadRequest) status code
        private ActionResult BadRequest(string content) => BadRequest(Jsonify(content));
        private ActionResult BadRequest(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.BadRequest, content.Data.ToString());

        // Ok takes a string or JSON object and returns it along with a 200 (OK) status code
        private ActionResult Ok(string content) => Ok(Jsonify(content));
        private ActionResult Ok(JsonResult content) =>
              new HttpStatusCodeResult(HttpStatusCode.OK, content.Data.ToString());

        //external pi requirements include requests, os, apscheduler
        // CTOR receives the DbHelper through Dependency Injection
        public PiAPIController(IDbHelper db) => _db = db;

        // GET: PiAPI
        //piapi
        public String Index()
        {
            return "I'm feeling Plant-Tastic!";
        }
        
        //POST: piapi/updateplant
        //ID,Water,Light
        [HttpPost]
        public void updateplant(Plant plant)
        {
            Plant currentPlant = _db.FindPlant(plant.Id);
            currentPlant.CurrentLight = plant.CurrentLight;
            currentPlant.CurrentWater = plant.CurrentWater;
            _db.UpdatePlant(currentPlant);
        }
    }
}