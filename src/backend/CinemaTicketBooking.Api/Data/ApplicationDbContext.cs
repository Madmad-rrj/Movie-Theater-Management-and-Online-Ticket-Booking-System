using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<CinemaRoom> CinemaRooms => Set<CinemaRoom>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cinema");

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("movie");
            entity.HasKey(movie => movie.Id);
            entity.Property(movie => movie.Id).HasColumnName("id");
            entity.Property(movie => movie.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            entity.Property(movie => movie.Description).HasColumnName("description");
            entity.Property(movie => movie.DurationMinutes).HasColumnName("duration_minutes").IsRequired();
            entity.Property(movie => movie.ReleaseDate).HasColumnName("release_date");
            entity.Property(movie => movie.PosterUrl).HasColumnName("poster_url");
            entity.Property(movie => movie.TrailerUrl).HasColumnName("trailer_url");
            entity.Property(movie => movie.Language).HasColumnName("language");
            entity.Property(movie => movie.AgeRating).HasColumnName("age_rating");
            entity.Property(movie => movie.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(movie => movie.Version).HasColumnName("version").IsRequired();
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("genre");
            entity.HasKey(genre => genre.Id);
            entity.Property(genre => genre.Id).HasColumnName("id");
            entity.Property(genre => genre.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
            entity.Property(genre => genre.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(genre => genre.Description).HasColumnName("description");
            entity.Property(genre => genre.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(genre => genre.Version).HasColumnName("version").IsRequired();
        });

        modelBuilder.Entity<MovieGenre>(entity =>
        {
            entity.ToTable("movie_genre");
            entity.HasKey(movieGenre => new { movieGenre.MovieId, movieGenre.GenreId });
            entity.Property(movieGenre => movieGenre.MovieId).HasColumnName("movie_id");
            entity.Property(movieGenre => movieGenre.GenreId).HasColumnName("genre_id");
            entity.HasOne(movieGenre => movieGenre.Movie)
                .WithMany(movie => movie.MovieGenres)
                .HasForeignKey(movieGenre => movieGenre.MovieId);
            entity.HasOne(movieGenre => movieGenre.Genre)
                .WithMany(genre => genre.MovieGenres)
                .HasForeignKey(movieGenre => movieGenre.GenreId);
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.ToTable("cinema");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
            entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(c => c.Address).HasColumnName("address").IsRequired();
            entity.Property(c => c.Phone).HasColumnName("phone");
            entity.Property(c => c.Timezone).HasColumnName("timezone").HasMaxLength(64).IsRequired();
            entity.Property(c => c.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(c => c.MinimumTurnaroundMinutes).HasColumnName("minimum_turnaround_minutes").IsRequired();
            entity.Property(c => c.Version).HasColumnName("version").IsRequired();
        });

        modelBuilder.Entity<CinemaRoom>(entity =>
        {
            entity.ToTable("cinema_room");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.CinemaId).HasColumnName("cinema_id").IsRequired();
            entity.Property(r => r.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
            entity.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(r => r.Version).HasColumnName("version").IsRequired();
            entity.HasOne(r => r.Cinema)
                .WithMany(c => c.Rooms)
                .HasForeignKey(r => r.CinemaId);
        });
    }
}
