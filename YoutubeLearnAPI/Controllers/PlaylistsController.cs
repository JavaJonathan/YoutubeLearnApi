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
                    p.CreatedAt,
                    p.CoreInsights
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
                .FirstOrDefaultAsync(v => v.Link.ToLower().Equals(link.ToLower()));

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

        [HttpPost("bulkUploadVideos")]
        public async Task<IActionResult> CreatePlaylistWithVideos([FromBody] CreatePlaylistWithVideosModel request)
        {
            if (request == null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title is required.");

            var title = request.Title.Trim();

            if (request.Videos.Count == 0)
                return BadRequest("At least one video with a valid Link is required.");

            var playlist = new YoutubePlaylist
            {
                Title = title,
                CreatedAt = DateTime.UtcNow
            };

            _db.YoutubePlaylists.Add(playlist);
            await _db.SaveChangesAsync();

            var normalizedIncomingLinks = request.Videos
                .Select(addVideoModel => addVideoModel.Link.ToLower())
                .Distinct()
                .ToList();

            var existingVideos = await _db.YoutubeVideos
                .Where(youtubeVideo => normalizedIncomingLinks.Contains(youtubeVideo.Link.ToLower()))
                .ToListAsync();

            var existingByLink = existingVideos.ToDictionary(
                youtubeVideo => youtubeVideo.Link,
                youtubeVideo => youtubeVideo,
                StringComparer.OrdinalIgnoreCase
            );

            var videosToInsert = new List<YoutubeVideo>();

            foreach (var addVideoModel in request.Videos)
            {
                if (existingByLink.ContainsKey(addVideoModel.Link))
                    continue;

                if (string.IsNullOrWhiteSpace(addVideoModel.Title))
                {
                    addVideoModel.Title = "Untitled";
                }

                if (string.IsNullOrWhiteSpace(addVideoModel.Channel))
                {
                    addVideoModel.Channel = "Unknown";
                }

                videosToInsert.Add(new YoutubeVideo
                {
                    Title = addVideoModel.Title,
                    Link = addVideoModel.Link,
                    Channel = addVideoModel.Channel,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (videosToInsert.Count > 0)
            {
                _db.YoutubeVideos.AddRange(videosToInsert);
                await _db.SaveChangesAsync();

                foreach (var insertedVideo in videosToInsert)
                {
                    existingByLink[insertedVideo.Link] = insertedVideo;
                }
            }

            var allVideoIds = existingByLink.Values.Select(youtubeVideo => youtubeVideo.Id).Distinct().ToList();

            var alreadyMappedVideoIds = await _db.PlaylistVideoMaps
                .Where(playlistVideoMap => playlistVideoMap.PlaylistId == playlist.Id && allVideoIds.Contains(playlistVideoMap.VideoId))
                .Select(playlistVideoMap => playlistVideoMap.VideoId)
                .ToListAsync();

            var alreadyMappedSet = new HashSet<Guid>(alreadyMappedVideoIds);

            var mapsToInsert = existingByLink.Values
                .Where(youtubeVideo => !alreadyMappedSet.Contains(youtubeVideo.Id))
                .Select(youtubeVideo => new PlaylistVideoMap
                {
                    PlaylistId = playlist.Id,
                    VideoId = youtubeVideo.Id
                })
                .ToList();

            if (mapsToInsert.Count > 0)
            {
                _db.PlaylistVideoMaps.AddRange(mapsToInsert);
                await _db.SaveChangesAsync();
            }

            var videosInPlaylist = await _db.PlaylistVideoMaps
                .Where(playlistVideoMap => playlistVideoMap.PlaylistId == playlist.Id)
                .Join(
                    _db.YoutubeVideos,
                    playlistVideoMap => playlistVideoMap.VideoId,
                    youtubeVideo => youtubeVideo.Id,
                    (playlistVideoMap, youtubeVideo) => new
                    {
                        youtubeVideo.Id,
                        youtubeVideo.Title,
                        youtubeVideo.Link,
                        youtubeVideo.Channel,
                        youtubeVideo.CreatedAt
                    }
                )
                .OrderByDescending(video => video.CreatedAt)
                .ToListAsync();

            return Ok(new
            {
                playlist = new
                {
                    playlist.Id,
                    playlist.Title,
                    playlist.CreatedAt
                },
                videos = videosInPlaylist,
                addedVideoCount = videosInPlaylist.Count
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

        [HttpPut("{playlistId:Guid}/coreInsights")]
        public async Task<IActionResult> UpdateCoreInsights(Guid playlistId, [FromBody] UpdatePlaylistCoreInsightsModel request)
        {
            if (request == null) return BadRequest("Body is required.");

            var normalizedCoreInsights = string.IsNullOrWhiteSpace(request.CoreInsights)
                ? null
                : request.CoreInsights.Trim();

            var playlist = await _db.YoutubePlaylists.FirstOrDefaultAsync(p => p.Id == playlistId);
            if (playlist == null) return NotFound("Playlist not found.");

            playlist.CoreInsights = normalizedCoreInsights;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                playlist.Id,
                playlist.Title,
                playlist.CreatedAt,
                playlist.CoreInsights
            });
        }
    }
}
