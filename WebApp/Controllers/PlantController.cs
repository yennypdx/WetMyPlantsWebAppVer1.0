using System.Net;
using System.Web.Mvc;
using DBHelper;
using Models;

namespace WebApp.Controllers
{
    [RoutePrefix("plant")]
    public class PlantController : Controller
    {
        private readonly IDbHelper _db;

        public PlantController(IDbHelper db) => _db = db;
        // GET: Plant
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }


         [HttpGet, Route("edit/{id?}")]
         public ActionResult Edit(string id = " ")
         {
             if (id == " ") return RedirectToAction("Index", "Home");
             // first, check if the user is logged in; if not, redirect to login
             var user = (User) Session["User"];
             if (user == null) return RedirectToAction("Login", "Home");

             // next, get the plant and make sure it belongs to the logged in user;
             // if not, redirect them back to their dashboard
             var plant = _db.FindPlant(id);
             if (!user.Plants.Contains(plant.Id)) return RedirectToAction("Index", "Home");

             // if everything is good, then get the list of species and stick in the ViewBag
             // and return the view with the Plant as the model.
             var speciesList = _db.GetAllSpecies();
             ViewBag.Species = speciesList;

             return View(plant);
         }
         //test to see if sensor mac address will work as id

        [AuthorizeUser, HttpGet, Route("update")]
        public ActionResult UpdatePlant(Plant plant)
        {
            if (!ModelState.IsValid) return RedirectToAction("Index", "Home");

            _db.UpdatePlant(plant);

            return RedirectToAction("Edit", "Plant", new {id = plant.Id});
        }

        [AuthorizeUser, HttpGet, Route("delete/{id}")]
        public ActionResult DeletePlant(int id)
        {
            if (Session["User"] is User user &&
                user.Plants.Exists(p => p.Equals(id)))
            {
                _db.DeletePlant(id);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}