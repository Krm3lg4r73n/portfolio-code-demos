using WebsiteBannwerk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebsiteBannwerk.Globalisation;
using System.Web.Security;
using WebMatrix.WebData;
using WebsiteBannwerk.Resources.Home;

namespace WebsiteBannwerk.Controllers
{
    public class HomeController : Controller
    {
        BannwerkSqlDb db = new BannwerkSqlDb();
        const int LOAD_NEWS_AMOUNT = 10;

        private HomeViewModel generateViewModel()
        {
            var viewModel = new HomeViewModel();
            string lang = CultureManager.GetLanguage();
            viewModel.NewsItems = db.News
                .Where(i => i.Language == lang)
                .OrderByDescending(i => i.TimeStamp)
                .Take(LOAD_NEWS_AMOUNT)
                .ToList();
            viewModel.AddNews = new AddNewsModel();

            if (viewModel.NewsItems.Count() < LOAD_NEWS_AMOUNT)
            {
                ViewBag.NewsCount = -1;
            }
            else
            {
                ViewBag.NewsCount = LOAD_NEWS_AMOUNT;
            }

            return viewModel;
        }

        public ActionResult Index()
        {
            return View(generateViewModel());
        }

        [Authorize(Roles = "master, authorNews")]
        public ActionResult AddNews(AddNewsModel model)
        {
            if(ModelState.IsValid)
            {
                try
                {
                    var membership = (SimpleMembershipProvider)Membership.Provider;
                    int userId = membership.GetUserId(User.Identity.Name);
                    var user = db.Users.First((u => u.UserId == userId));

                    if (model.Body == null)
                        model.Body = "";
                    
                    if(model.Id < 0)
                    {
                        //add news
                        var item = new NewsItem();
                        item.Author = user.UserName;
                        item.Headline = model.Headline.Replace(System.Environment.NewLine, "<br />");
                        item.Teaser = model.Teaser.Replace(System.Environment.NewLine, "<br />");
                        item.Body = model.Body.Replace(System.Environment.NewLine, "<br />");
                        item.Language = CultureManager.GetLanguage();
                        item.TimeStamp = DateTime.Now;
                        db.News.Add(item);
                        db.SaveChanges();
                    }
                    else
                    {
                        //edit news
                        NewsItem item = db.News.Where(i => i.Id == model.Id).First();
                        item.Headline = model.Headline.Replace(System.Environment.NewLine, "<br />");
                        item.Teaser = model.Teaser.Replace(System.Environment.NewLine, "<br />");
                        item.Body = model.Body.Replace(System.Environment.NewLine, "<br />");
                        item.Body +=
                            "<br /><span class='newsEditFormat'>" +
                            String.Format(Misc.NewsEditFormat, user.UserName, DateTime.Now) +
                            "</span>";
                        db.SaveChanges();
                    }

                    return RedirectToAction("Index");
                }
                catch(Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            var viewModel = generateViewModel();
            viewModel.AddNews = model;
            return View("Index", viewModel);
        }

        [Authorize(Roles = "master, authorNews")]
        public ActionResult DeleteNews(int id)
        {
            NewsItem item = db.News.Where(i => i.Id == id).First();
            db.News.Remove(item);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "master, authorNews")]
        public ActionResult EditNews(int id)
        {
            NewsItem item = db.News.Where(i => i.Id == id).First();

            if(item == null)
                return RedirectToAction("Index");

            var viewModel = generateViewModel();
            viewModel.AddNews.Headline  = item.Headline.Replace("<br />", System.Environment.NewLine);
            viewModel.AddNews.Teaser    = item.Teaser.Replace("<br />", System.Environment.NewLine);
            viewModel.AddNews.Body      = item.Body.Replace("<br />", System.Environment.NewLine);
            viewModel.AddNews.TimeStamp = item.TimeStamp;
            viewModel.AddNews.Id        = item.Id;

            return View("Index", viewModel);
        }

        public ActionResult LoadNews(int newsCount)
        {
            string lang = CultureManager.GetLanguage();
            IEnumerable<NewsItem> newsItems = db.News
                .Where(i => i.Language == lang)
                .OrderByDescending(i => i.TimeStamp)
                .Skip(newsCount)
                .Take(LOAD_NEWS_AMOUNT)
                .ToList();
            if (newsItems.Count() < LOAD_NEWS_AMOUNT)
            {
                ViewBag.NewsCount = -1;
            }
            else
            {
                ViewBag.NewsCount = newsCount + LOAD_NEWS_AMOUNT;
            }

            return PartialView("_NewsList", newsItems);
        }

        protected override void Dispose(bool disposing)
        {
            if (db != null)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
