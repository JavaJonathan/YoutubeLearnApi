using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using YoutubeLearnAPI.Data;
using YoutubeLearnAPI.Models;

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

        [HttpPost]
        public async Task<IActionResult> CreatePlaylist([FromBody] CreatePlaylistModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");

            var title = request.Title.Trim();

            var playlist = new YoutubePlaylist
            {
                Title = title,
                CreatedAt = DateTime.UtcNow
            };

            _db.YoutubePlaylists.Add(playlist);
            await _db.SaveChangesAsync();

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

        [HttpPost("{playlistId:Guid}/videos")]
        public async Task<IActionResult> AddVideoToPlaylist(Guid playlistId, [FromBody] AddVideoModel request)
        {
            if (request == null) return BadRequest("Body is required.");

            var title = request.Title?.Trim();
            var link = request.Link?.Trim();
            var channel = request.Channel?.Trim();

            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(link)) return BadRequest("Link is required.");

            var playlist = await _db.YoutubePlaylists.FirstOrDefaultAsync(p => p.Id == playlistId);
            if (playlist == null) return NotFound("Playlist not found.");

            var video = await _db.YoutubeVideos
                .FirstOrDefaultAsync(v => v.Link.Equals(link, StringComparison.OrdinalIgnoreCase));

            var videoAlreadyInPlaylist = await _db.PlaylistVideoMaps
                .AnyAsync(pvm => pvm.PlaylistId == playlistId && video != null && pvm.VideoId == video.Id);

            if (videoAlreadyInPlaylist) return Conflict("This video already exists in the playlist.");

            if (video == null)
            {
                video = new YoutubeVideo
                {
                    Title = title,
                    Link = link,
                    Channel = channel,
                    CreatedAt = DateTime.UtcNow
                };

                _db.YoutubeVideos.Add(video);
                await _db.SaveChangesAsync();
            }

            var map = new PlaylistVideoMap
            {
                PlaylistId = playlist.Id,
                VideoId = video.Id
            };

            _db.PlaylistVideoMaps.Add(map);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                video.Id,
                playlistId = playlist.Id,
                video.Title,
                video.Link,
                video.Channel,
                video.CreatedAt
            });
        }

        [HttpDelete("{playlistId:Guid}/videos/{videoId:Guid}")]
        public async Task<IActionResult> RemoveVideoFromPlaylist(Guid playlistId, Guid videoId)
        {
            var playlistExists = await _db.YoutubePlaylists.AnyAsync(p => p.Id == playlistId);
            if (!playlistExists) return NotFound("Playlist not found.");

            var map = await _db.PlaylistVideoMaps
                .FirstOrDefaultAsync(m => m.PlaylistId == playlistId && m.VideoId == videoId);

            if (map == null) return NotFound("Video not found in this playlist.");

            _db.PlaylistVideoMaps.Remove(map);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
