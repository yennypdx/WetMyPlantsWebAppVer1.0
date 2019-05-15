using DbHelper;
using Models;
using System.Web.Mvc;
using WebApp.Auth;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class StatusResponse
    {
        public bool Success { get; set; }
        public bool Error { get; set; }
    }
    [RoutePrefix("plant")]
    public class PlantController : AuthController
    {
        //private readonly IDbHelper _db;

        public PlantController(IDbHelper db) : base(db) { }

        // GET: Plant
        [AuthorizeUser]
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        [AuthorizeUser]
        [HttpGet, Route("new")]
        public ActionResult New()
        {
            var species = Db.GetAllSpecies();
            ViewBag.Species = species;

            return View("Add");
        }

        [HttpPost, AuthorizeUser, ValidateAntiForgeryToken, Route("new")]
        public ActionResult CreatePlant(Plant plant)
        {
            if(ModelState.IsValid)
            {
                Db.CreateNewPlant(plant.Id, plant.SpeciesId, plant.Nickname, plant.CurrentWater, plant.CurrentLight);
                var user = Session["User"] as User;
                Db.RegisterPlantToUser(plant, user);
            }

            return RedirectToAction("Index", "Home");
        }



        [HttpGet, Route("edit")]
        public ActionResult Edit(string id = " ")
        {
            var message = TempData["Message"] as StatusResponse;

            if(id == " ")
            {
                return RedirectToAction("Index", "Home");
            }
            // first, check if the user is logged in; if not, redirect to login
            var user = (User)Session["User"];
            if(user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // next, get the plant and make sure it belongs to the logged in user;
            // if not, redirect them back to their dashboard
            var plant = Db.FindPlant(id);
            if(!user.Plants.Contains(plant.Id))
            {
                return RedirectToAction("Index", "Home");
            }

            // if everything is good, then get the list of species and stick in the ViewBag
            // and return the view with the Plant as the model.
            var speciesList = Db.GetAllSpecies();
            ViewBag.Species = speciesList;
            ViewBag.Message = message;

            return View(plant);
        }
        //test to see if sensor mac address will work as id

        [AuthorizeUser, HttpPost, Route("update")]
        public ActionResult UpdatePlant(Plant plant)
        {
            if(!ModelState.IsValid)
            {
                return RedirectToAction("Index", "Home");
            }

            var ob = new StatusResponse();

            if (Db.UpdatePlant(plant))
                ob.Success = true;
            else
                ob.Error = true;

            TempData["Message"] = ob;

            return RedirectToAction("Edit", "Plant", new { id = plant.Id });
        }

        [AuthorizeUser, HttpGet, Route("delete")]
        public ActionResult DeletePlant(string id)
        {
            var user = Session["User"] as User;
            var plant = user != null && user.Plants.Contains(id)
                ? Db.FindPlant(id)
                : null;

            if(user != null && plant != null)
            {
                Db.DeletePlant(id);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}