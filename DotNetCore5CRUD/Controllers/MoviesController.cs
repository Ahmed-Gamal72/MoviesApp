using DotNetCore5CRUD.Models;
using DotNetCore5CRUD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCore5CRUD.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDBContext _context;
        private List<string> _allowedExtension = new List<string> { ".png", ".jpg" };
        private long _maxAllowedPosterSize = 1048576;
        public MoviesController(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies.ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new MovieFormViewModel
            {
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };
            return View("MovieForm", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }
            var files = Request.Form.Files;
            if (!files.Any())
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Please select movie poster!");
                return View("MovieForm", model);
            }

            var poster = files.FirstOrDefault();
            if (!_allowedExtension.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .JPG, .PNG images are allowed!");
                return View("MovieForm", model);
            }

            if (poster.Length > _maxAllowedPosterSize)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB!");
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
                StoryLine = model.StoryLine,
                Poster = dataStream.ToArray()
            };

            _context.Movies.Add(movies);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return BadRequest();
            var movie = await _context.Movies.FindAsync(id);

            if (movie == null)
                return NotFound();
            var viewModel = new MovieFormViewModel
            {
                Id = movie.Id,
                Title = movie.Title,
                GenreId = movie.GenreId,
                Year = movie.Year,
                Rate = movie.Rate,
                StoryLine = movie.StoryLine,
                Poster = movie.Poster,
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };
            return View("MovieForm", viewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }

            var movie = await _context.Movies.FindAsync(model.Id);

            if (movie == null)
                return NotFound();

            var files = Request.Form.Files;
            if (files.Any())
            {

                var poster = files.FirstOrDefault();
                using var datastream = new MemoryStream();
                await poster.CopyToAsync(datastream);

                model.Poster = datastream.ToArray();
                if (!_allowedExtension.Contains(Path.GetExtension(poster.FileName).ToLower()))
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only .JPG, .PNG images are allowed!");
                    return View("MovieForm", model);
                }

                if (poster.Length > _maxAllowedPosterSize)
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster cannot be more than 1 MB!");
                    return View("MovieForm", model);
                }

                movie.Poster = model.Poster;
            }

            movie.Title = model.Title;
            movie.GenreId = model.GenreId;
            movie.Year = model.Year;
            movie.Rate = model.Rate;
            movie.StoryLine = model.StoryLine;
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

    }
}
