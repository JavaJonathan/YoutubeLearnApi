using Microsoft.EntityFrameworkCore;
using YoutubeLearnAPI.Models;

namespace YoutubeLearnAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<YoutubePlaylist> YoutubePlaylists => Set<YoutubePlaylist>();
    public DbSet<YoutubeVideo> YoutubeVideos => Set<YoutubeVideo>();
    public DbSet<VideoTag> VideoTags => Set<VideoTag>();
    public DbSet<PlaylistVideoMap> PlaylistVideoMaps => Set<PlaylistVideoMap>();
    public DbSet<VideoTagMap> VideoTagMaps => Set<VideoTagMap>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<YoutubePlaylist>(e =>
        {
            e.ToTable("YoutubePlaylists");

            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.CoreInsights);

            // Relationship: Playlist -> PlaylistVideoMaps
            e.HasMany(x => x.PlaylistVideoMaps)
             .WithOne(x => x.Playlist)
             .HasForeignKey(x => x.PlaylistId);
        });

        modelBuilder.Entity<YoutubeVideo>(e =>
        {
            e.ToTable("YoutubeVideos");

            e.HasKey(x => x.Id);
            e.Property(x => x.Link).IsRequired().HasMaxLength(500);
            e.Property(x => x.Channel).IsRequired().HasMaxLength(500);
            e.Property(x => x.Title).IsRequired().HasMaxLength(500);
            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.CoreInsights).HasColumnType("nvarchar(max)");
            e.Property(x => x.Impact);

            e.HasMany(x => x.PlaylistVideoMaps)
             .WithOne(x => x.Video)
             .HasForeignKey(x => x.VideoId);

            e.HasMany(x => x.VideoTagMaps)
             .WithOne(x => x.Video)
             .HasForeignKey(x => x.VideoId);
        });

        modelBuilder.Entity<VideoTag>(e =>
        {
            e.ToTable("VideoTags");

            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(50);

            e.HasMany(x => x.VideoTagMaps)
             .WithOne(x => x.Tag)
             .HasForeignKey(x => x.TagId);
        });

        modelBuilder.Entity<PlaylistVideoMap>(e =>
        {
            e.ToTable("PlaylistVideoMaps");

            e.HasKey(x => x.Id);
            e.Property(x => x.PlaylistId).IsRequired();
            e.Property(x => x.VideoId).IsRequired();
        });

        modelBuilder.Entity<VideoTagMap>(e =>
        {
            e.ToTable("VideoTagMaps");

            e.HasKey(x => x.Id);
            e.Property(x => x.VideoId).IsRequired();
            e.Property(x => x.TagId).IsRequired();
        });
    }
}
