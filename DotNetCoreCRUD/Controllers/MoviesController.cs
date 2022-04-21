using DotNetCoreCRUD.Models;
using DotNetCoreCRUD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCoreCRUD.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        private new List<string> _allowedExtenstions = new List<string> { ".jpg", ".png" };
        private long _maxAllowedPosterSize = 1048576;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var movie = await _context.Movies.OrderByDescending(m=>m.Rate).ToListAsync();
            return View(movie);
        }

        //دي الميثود اللي هتروح كريت فيوالي في موفيز
        public async Task<IActionResult> Create()
        {
            var ViewModel = new MovieFormViewModel
            {
                Momo = await _context.Genres.OrderBy(m=>m.Name).ToListAsync()
            };
           

            return View("MovieForm",ViewModel);
        }
        //البيانات اللي في الفورم اللي في كريت فيو بتيجي هنا في الميثود دي اللي من نوع بوست
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {

            // #first_step
            //بشوف لو البيانات اللي مرسله مش مظبوطه يرجع للفورم تاني 
            //وهرجع مع بيانات الموديل بيانات الجينريز عشان يعرف يستعرضها في حقل الجينريز
           if(!ModelState.IsValid)

            {
                model.Momo = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }
           //#second_step
           //(دلوقتي خلاص أتاكدنا ان البيانات فالد او مظبوطه يعني نشيك بقا ان في صوره مبعوته ولا لا (البوستر
           //انا بقوله هنا لو مفيش فايلات مبعوته يرجع للفورم تاني بنفس البياناتالمدخله بس يطلع رساله في الحقل البوستر انه لازم يدخل بوستر
           //وطبعا هرجع الجينيرز عشان تظهر في قائمه الجينريز
            var Files = Request.Form.Files;
            if(!Files.Any())
            {
                model.Momo = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "pls Select Poster");
                return View("MovieForm", model);
            }
            //#third_step
            //خلا طلع ان اليزر باعت ملف فهتأكد ان الملف دا من النوع جيبجي او بنج
            var poster = Files.FirstOrDefault();
            


            if (!_allowedExtenstions.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {

                model.Momo = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .PNG, .JPG images are allowed!");
                return View("MovieForm", model);
            }
            //هنا بتأكد ان الصوره مش أكبر من 1 ميجا بايت
            if(poster.Length> _maxAllowedPosterSize)
            {
                model.Momo = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Sorry Your Poster Greater Than 1 MB!");
                return View("MovieForm", model);
            }

            using var dataStream = new MemoryStream();
            await poster.CopyToAsync(dataStream);

            var movies = new Movie
            {
                Title = model.Title,
                GenreId = model.GenreId,
                Year = model.Year,
                Rate = model.Rate,
                StoreLine = model.Storeline,
                Poster = dataStream.ToArray()
            };

            _context.Movies.Add(movies);
            _context.SaveChanges();

           // _toastNotification.AddSuccessToastMessage("Movie created successfully");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int?id)
        {
            if (id == null)
                return BadRequest();
            var Movie = await _context.Movies.FindAsync(id);

            if (Movie == null)
                return NotFound();

            var varable = new MovieFormViewModel
            {
                Id      =   Movie.Id,
                Title   =   Movie.Title,
                GenreId =   Movie.GenreId,
                Rate    =   Movie.Rate,
                Year    =   Movie.Year,
                Storeline = Movie.StoreLine,
                Poster  =   Movie.Poster,
                Momo    = await _context.Genres.OrderBy(m => m.Name).ToListAsync()

            };
            return View("MovieForm", varable);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {

            if (!ModelState.IsValid)
            {
                model.Momo = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }

            var Movie = await _context.Movies.FindAsync(model.Id);

            if (Movie == null)
                return NotFound();


            var files = Request.Form.Files;

            if (files.Any())
            {
                var poster = files.FirstOrDefault();

                using var dataStream = new MemoryStream();

                await poster.CopyToAsync(dataStream);

                model.Poster = dataStream.ToArray();

                if (!_allowedExtenstions.Contains(Path.GetExtension(poster.FileName).ToLower()))
                {
                    model.Momo = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only .PNG, .JPG images are allowed!");
                    return View("MovieForm", model);
                }

                if (poster.Length > _maxAllowedPosterSize)
                {
                    model.Momo = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB!");
                    return View("MovieForm", model);
                }

                Movie.Poster = model.Poster;
            }

            Movie.Id = model.Id;
            Movie.Title = model.Title;
            Movie.GenreId = model.GenreId;
            Movie.Rate = model.Rate;
            Movie.Year = model.Year;
            Movie.StoreLine = model.Storeline;

            _context.SaveChanges();


            //  _toastNotification.AddSuccessToastMessage("Movie updated successfully");
                return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.Include(m => m.Genre).SingleOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            return View(movie);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();

            _context.Movies.Remove(movie);
            _context.SaveChanges();

            return Ok();
        }
    }
}
