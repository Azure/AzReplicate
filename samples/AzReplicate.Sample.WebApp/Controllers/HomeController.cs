using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AzReplicate.Sample.WebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace AzReplicate.Sample.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ImageContext _dbcontext;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(ILogger<HomeController> logger,
            IWebHostEnvironment hostingEnvironment,
            ImageContext dbcontext)
        {
            _logger = logger;
            _dbcontext = dbcontext;
            _hostingEnvironment = hostingEnvironment;
        }

        /// <summary>
        /// Get a random list of 25 images URLs from the database to show
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            return View(_dbcontext.Images.OrderBy(x => Guid.NewGuid()).Take(25).ToList());
        }

        /// <summary>
        /// Seed/reseed the database with a bunch of image URLs from S3. 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Reset(int? count)
        {
            int row = 0;
            string line;
            var filepath = Path.Combine(_hostingEnvironment.WebRootPath, "sample-image-urls.txt");

            if (!count.HasValue)
                count = 50;

            //Clear the database
            await _dbcontext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Images");

            //read the file with a list of URLs from the disk
            using (StreamReader sr = new StreamReader(filepath))
            {
                while ((line = sr.ReadLine()) != null && row < count)
                {
                    _dbcontext.Images.Add(new Image() { Url = line });
                    row++;
                }
            }
            await _dbcontext.SaveChangesAsync();

            //redirect the user back to the home page
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


    }
}
