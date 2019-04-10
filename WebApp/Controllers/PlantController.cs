using System.Collections.Generic;
using System.Web.Mvc;
using DBHelper;
using Models;
using WebApp.Auth;

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

        [AuthorizeUser, HttpGet, Route("edit/{id?}")]
        public ActionResult Edit(int id = 0)
        {
            if (id == 0) return RedirectToAction("Index", "Home");
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

        [AuthorizeUser, HttpGet, Route("update")]
        public ActionResult UpdatePlant(Plant plant)
        {
            if (!ModelState.IsValid) return RedirectToAction("Index", "Home");

            _db.UpdatePlant(plant);

            return RedirectToAction("Edit", "Plant", new {id = plant.Id});
        }
    }
}