using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using YoutubeLearnAPI.Data;
using YoutubeLearnAPI.Models;

namespace YoutubeLearnAPI.Controllers
{
    [ApiController]
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TagsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Tag name is required.");

            var tagName = request.Name.Trim();

            var normalizedName = tagName.ToLower();

            var tagAlreadyExists = await _db.VideoTags
                .AnyAsync(existingTag =>
                    existingTag.Title.ToLower() == normalizedName);

            if (tagAlreadyExists)
                return Conflict("A tag with this name already exists.");

            var tag = new VideoTag
            {
                Title = tagName
            };

            _db.VideoTags.Add(tag);
            await _db.SaveChangesAsync();

            var tags = await _db.VideoTags
                .OrderBy(tagEntity => tagEntity.Title)
                .Select(tagEntity => new
                {
                    tagEntity.Id,
                    tagEntity.Title
                })
                .ToListAsync();

            return Ok(tags);
        }
    }
}
