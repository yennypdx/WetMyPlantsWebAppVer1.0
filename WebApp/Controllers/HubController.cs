using DbHelper;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApp.Auth;
using WebApp.Models;
using WebApp.Models.HubViewModels;

namespace WebApp.Controllers
{
    public class HubController : AuthController
    {
        public HubController(IDbHelper db) : base(db) { }
        // GET: Hub
        [AuthorizeUser]
        public ActionResult Index()
        {
            var userId = (Session["User"] as User).Id;

            var hubs = Db.GetHubList(userId);

            var hubViewModel = new HubViewModel
            {
                Hubs = hubs
            };
            return View(hubViewModel);
        }

        /*
        // GET: Hub/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }
        */

        // GET: Hub/Create
        public ActionResult Create()
        {
            return View();
        }

        /*
        // POST: Hub/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Hub/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Hub/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        */

        // GET: Hub/Delete/5
        [AuthorizeUser]
        public ActionResult Delete(int id)
        {
            var hub = Db.GetHub(id);
            var user = Session["User"] as User;

            if (hub.UserId == user?.Id)
                Db.DeleteHub(id);

            return View("Index");
        }

        /*
        // POST: Hub/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
        */
    }
}
