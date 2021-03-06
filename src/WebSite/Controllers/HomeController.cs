﻿using Core;
using Core.Managers;
using Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using X.PagedList;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Collections.Generic;

namespace WebSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly PublicationManager _publicationManager;
        private readonly VacancyManager _vacancyManager;
        private readonly IHostingEnvironment _env;
        private readonly IMemoryCache _cache;

        public HomeController(IMemoryCache cache, IHostingEnvironment env)
        {
            _cache = cache;
            _publicationManager = new PublicationManager(Core.Settings.Current.ConnectionString, cache);
            _vacancyManager = new VacancyManager(Core.Settings.Current.ConnectionString, cache);
            _env = env;
        }

        public override void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            LoadHotVacanciesToViewData();
        }

        private void LoadHotVacanciesToViewData()
        {
            var vacancies = _vacancyManager
                                .GetHotVacancies()
                                .Select(o => new VacancyViewModel(o, Settings.Current.WebSiteUrl))
                                .ToList();

            ViewData["vacancies"] = vacancies;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Welcome!";

            var pagedResult = await _publicationManager.GetPublications();
            var categories = _publicationManager.GetCategories();
            var model = new StaticPagedList<PublicationViewModel>(pagedResult.Select(o => new PublicationViewModel(o, Settings.Current.WebSiteUrl, categories)), pagedResult);

            return View(model);
        }

        [Route("page/{page}")]
        public async Task<IActionResult> Page(int? categoryId = null, int page = 1, string lanugage = Core.Language.English)
        {
            ViewData["Title"] = $"{Core.Pages.Page} {page}";

            var pagedResult = await _publicationManager.GetPublications(categoryId, page);
            var categories = _publicationManager.GetCategories();
            var model = new StaticPagedList<PublicationViewModel>(pagedResult.Select(o => new PublicationViewModel(o, Settings.Current.WebSiteUrl, categories)), pagedResult);

            ViewBag.CategoryId = categoryId;

            return View("~/Views/Home/Page.cshtml", model);
        }

        [Route("vacancies/{page}")]
        public async Task<IActionResult> Vacancies(int page = 1)
        {
            ViewData["Title"] = $"{Core.Pages.Vacancies}";

            var pagedResult = await _vacancyManager.GetVacancies(page);

            var model = new StaticPagedList<VacancyViewModel>(pagedResult.Select(o => new VacancyViewModel(o, Settings.Current.WebSiteUrl)), pagedResult);

            return View("~/Views/Home/Vacancies.cshtml", model);
        }

        [Route("vacancy/{id}")]
        public async Task<IActionResult> Vacancy(int id)
        {
            var vacancy = await _vacancyManager.Get(id);

            if (vacancy == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound);
            }

            var path = Path.Combine(_env.WebRootPath, "images/vacancy");
            var file = Directory.GetFiles(path).OrderBy(o => Guid.NewGuid()).Select(o => Path.GetFileName(o)).FirstOrDefault();
            var image = $"{Settings.Current.WebSiteUrl}images/vacancy/{file}";

            var model = new VacancyViewModel(vacancy, Settings.Current.WebSiteUrl, image);
            ViewData["Title"] = model.Title;

            return View("~/Views/Home/Vacancy.cshtml", model);
        }

        [Route("post/{id}")]
        public async Task<IActionResult> Post(int id)
        {
            var publication = await _publicationManager.Get(id);

            if (publication == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound);
            }

            var model = new PublicationViewModel(publication, Settings.Current.WebSiteUrl);
            ViewData["Title"] = model.Title;

            return View("~/Views/Home/Post.cshtml", model);
        }

        public async Task<IActionResult> Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}