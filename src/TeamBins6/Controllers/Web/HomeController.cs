﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.Configuration;
using StackExchange.Exceptional;
using TeamBins.Common;
using TeamBins.Services;
using TeamBins6.Infrastrucutre.Services;

namespace TeamBins6.Controllers
{

    public class BaseController : Controller
    {
        public string AppBaseUrl
        {
            get
            {
                return string.Format("{0}://{1}{2}", Request.Scheme, Request.Host, Url.Content("~"));
            }
        }

    }
    public class HomeController : BaseController
    {
        private IUserSessionHelper userSessionHelper;
        private IUserAccountManager userAccountManager;
        private IConfiguration configuration; 

        public HomeController(IUserSessionHelper userSessionHelper,IUserAccountManager userAccountManager,IConfiguration configuration)
        {
            this.userSessionHelper = userSessionHelper;
            this.userAccountManager = userAccountManager;
            this.configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> SwitchTeam(int teamId)
        {

            userSessionHelper.SetTeamId(teamId);
            await userAccountManager.SetDefaultTeam(userSessionHelper.UserId, teamId);

            return Json("Changed");
        }

        public IActionResult Index()
        {
           // this.userSessionHelper.SetUserIDToSession(new LoggedInSessionInfo { TeamId = 13109, UserId = 12095 });

            if (this.userSessionHelper.UserId > 0)
            {
                return RedirectToAction("Index", "Issue");
            }


            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
