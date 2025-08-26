using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TourismManagementSystem.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            ViewBag.ActivePage = "Home";
            return View();
        }

        public ActionResult About()
        {
            ViewBag.ActivePage = "About";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.ActivePage = "Contact";

            return View();
        }
    }
}