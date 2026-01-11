using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using YoutubeLearnAPI.Data;

namespace YoutubeLearnAPI.Controllers
{
    [ApiController]
    [Route("api/playlists")]
    public class PlaylistsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PlaylistsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var playlists = await _db.YoutubePlaylists
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(playlists);
        }
    }
}
