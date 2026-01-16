using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using YoutubeLearnAPI.Data;
using YoutubeLearnAPI.Models;

namespace YoutubeLearnAPI.Controllers
{
    // TO DO: Add ability to mark video as reviewed and keep a review history
    [ApiController]
    [Route("api/video")]
    public class VideoController : ControllerBase
    {
        private readonly AppDbContext _db;

        public VideoController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetVideos(
            [FromQuery] Guid? playlistId,
            [FromQuery] string[] tags,
            [FromQuery] bool matchAllTags = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200;

            IQueryable<PlaylistVideoMap> query = _db.PlaylistVideoMaps;

            if (playlistId.HasValue)
            {
                query = query.Where(videoMap => videoMap.PlaylistId == playlistId.Value);
            }

            List<string> tagList = new List<string>();

            if (tagList.Count > 0)
            {
                if (!matchAllTags)
                {
                    // any of the tags
                    query = query.Where(v =>
                        _db.VideoTagMaps
                            .Where(m => m.VideoId == v.Id)
                            .Join(_db.VideoTags, m => m.TagId, t => t.Id, (m, t) => t.Title)
                            .Any(tagName => tagList.Contains(tagName))
                    );
                }
                else
                {
                    // must contain ALL tags
                    query = query.Where(v =>
                        _db.VideoTagMaps
                            .Where(m => m.VideoId == v.Id)
                            .Join(_db.VideoTags, m => m.TagId, t => t.Id, (m, t) => t.Title.ToLower())
                            .Distinct()
                            .Count(tagName => tagList.Contains(tagName)) == tagList.Count
                    );
                }
            }

            var total = await query.CountAsync();

            var videos = await query
                .OrderByDescending(v => v.Video.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.Video.Id,
                    v.PlaylistId,
                    v.Video.Title,
                    v.Video.Link,
                    v.Video.Channel,
                    v.Video.CreatedAt,
                    v.Video.CoreInsights,
                    Tags = _db.VideoTagMaps
                        .Where(m => m.VideoId == v.Video.Id)
                        .Join(_db.VideoTags, m => m.TagId, t => t.Id, (m, t) => new
                        {
                            t.Id,
                            t.Title
                        })
                        .OrderBy(t => t.Title)
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = videos
            });
        }

        [HttpPost("{videoId:Guid}/core-insights")]
        public async Task<IActionResult> UpdateCoreInsights(Guid videoId, [FromBody] UpdateCoreInsightsModel request)
        {
            var video = await _db.YoutubeVideos.FirstOrDefaultAsync(v => v.Id == videoId);
            if (video == null) return NotFound("Video not found.");

            if ( string.IsNullOrEmpty( request.CoreInsights ) ) return BadRequest("Core insights payload is empty.");

            video.CoreInsights = request.CoreInsights;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                video.Id,
                video.CoreInsights
            });
        }

        [HttpPut("{videoId:Guid}/tags")]
        public async Task<IActionResult> UpdateTags(Guid videoId, [FromBody] UpdateTagsModel request)
        {
            if (request?.Tags == null || request.Tags.Count == 0)
                return BadRequest("Tags are required.");

            var videoExists = await _db.YoutubeVideos.AnyAsync(v => v.Id == videoId);
            if (!videoExists) return NotFound("Video not found.");

            var alreadyLinked = await _db.VideoTagMaps
                .Where(x => x.VideoId == videoId && request.Tags.Contains(x.TagId))
                .Select(x => x.TagId)
                .ToListAsync();

            var alreadyLinkedSet = alreadyLinked.ToHashSet();

            var newLinks = request.Tags
                .Where(tagId => !alreadyLinkedSet.Contains(tagId))
                .Select(tagId => new VideoTagMap
                {
                    VideoId = videoId,
                    TagId = tagId
                })
                .ToList();

            if (newLinks.Count == 0)
            {
                var current = await _db.VideoTagMaps
                    .Where(x => x.VideoId == videoId)
                    .Join(_db.VideoTags, x => x.TagId, t => t.Id, (x, t) => new { t.Id, t.Title })
                    .OrderBy(t => t.Title)
                    .ToListAsync();

                return Ok(new { VideoId = videoId, Tags = current });
            }

            _db.VideoTagMaps.AddRange(newLinks);
            await _db.SaveChangesAsync();

            var updated = await _db.VideoTagMaps
                .Where(x => x.VideoId == videoId)
                .Join(_db.VideoTags, x => x.TagId, t => t.Id, (x, t) => new { t.Id, t.Title })
                .OrderBy(t => t.Title)
                .ToListAsync();

            return Ok(new { VideoId = videoId, Tags = updated });
        }        
    }
}
