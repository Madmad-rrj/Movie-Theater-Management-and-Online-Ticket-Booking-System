using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<CheckoutIdentity> CheckoutIdentities => Set<CheckoutIdentity>();
    public DbSet<OperationReceipt> OperationReceipts => Set<OperationReceipt>();
    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<CinemaRoom> CinemaRooms => Set<CinemaRoom>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<TicketCategory> TicketCategories => Set<TicketCategory>();
    public DbSet<CinemaTicketPrice> CinemaTicketPrices => Set<CinemaTicketPrice>();
    public DbSet<SeatType> SeatTypes => Set<SeatType>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cinema");
        modelBuilder.ConfigureGuestCheckout();
        modelBuilder.ConfigureSelections();
        modelBuilder.ConfigureBookings();

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

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.ToTable("showtime");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.SeedKey).HasColumnName("seed_key");
            entity.Property(s => s.MovieId).HasColumnName("movie_id").IsRequired();
            entity.Property(s => s.CinemaRoomId).HasColumnName("cinema_room_id").IsRequired();
            entity.Property(s => s.DurationMinutesSnapshot).HasColumnName("duration_minutes_snapshot").IsRequired();
            entity.Property(s => s.TurnaroundMinutesApplied).HasColumnName("turnaround_minutes_applied").IsRequired();
            entity.Property(s => s.StartsAt).HasColumnName("starts_at").IsRequired();
            entity.Property(s => s.EndsAt).HasColumnName("ends_at").IsRequired();
            entity.Property(s => s.RoomAvailableAt).HasColumnName("room_available_at").IsRequired();
            entity.Property(s => s.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(s => s.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(s => s.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(s => s.Version).HasColumnName("version").IsRequired();
            entity.HasOne(s => s.Movie)
                .WithMany()
                .HasForeignKey(s => s.MovieId);
            entity.HasOne(s => s.CinemaRoom)
                .WithMany(r => r.Showtimes)
                .HasForeignKey(s => s.CinemaRoomId);
        });

        modelBuilder.Entity<TicketCategory>(entity =>
        {
            entity.ToTable("ticket_category");
            entity.HasKey(t => t.Code);
            entity.Property(t => t.Code).HasColumnName("code").HasMaxLength(16);
            entity.Property(t => t.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            entity.Property(t => t.EligibilityDescription).HasColumnName("eligibility_description").IsRequired();
            entity.Property(t => t.Version).HasColumnName("version").IsRequired();
        });

        modelBuilder.Entity<CinemaTicketPrice>(entity =>
        {
            entity.ToTable("cinema_ticket_price");
            entity.HasKey(p => new { p.CinemaId, p.CategoryCode });
            entity.Property(p => p.CinemaId).HasColumnName("cinema_id");
            entity.Property(p => p.CategoryCode).HasColumnName("category_code").HasMaxLength(16);
            entity.Property(p => p.Amount).HasColumnName("amount").HasColumnType("numeric").IsRequired();
            entity.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            entity.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(p => p.Version).HasColumnName("version").IsRequired();
            entity.HasOne(p => p.Cinema)
                .WithMany(c => c.TicketPrices)
                .HasForeignKey(p => p.CinemaId);
            entity.HasOne(p => p.Category)
                .WithMany(c => c.CinemaTicketPrices)
                .HasForeignKey(p => p.CategoryCode);
        });

        modelBuilder.Entity<SeatType>(entity =>
        {
            entity.ToTable("seat_type");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
            entity.Property(s => s.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(s => s.SurchargeAmount).HasColumnName("surcharge_amount").HasColumnType("numeric").IsRequired();
            entity.Property(s => s.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(s => s.Version).HasColumnName("version").IsRequired();
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("seat");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).HasColumnName("id");
            entity.Property(s => s.CinemaRoomId).HasColumnName("cinema_room_id").IsRequired();
            entity.Property(s => s.SeatTypeId).HasColumnName("seat_type_id").IsRequired();
            entity.Property(s => s.RowLabel).HasColumnName("row_label").HasMaxLength(10).IsRequired();
            entity.Property(s => s.SeatNumber).HasColumnName("seat_number").IsRequired();
            entity.Property(s => s.XPosition).HasColumnName("x_position").IsRequired();
            entity.Property(s => s.YPosition).HasColumnName("y_position").IsRequired();
            entity.Property(s => s.OperationalState).HasColumnName("operational_state").HasMaxLength(20).IsRequired();
            entity.Property(s => s.RetiredAt).HasColumnName("retired_at");
            entity.Property(s => s.Version).HasColumnName("version").IsRequired();
            entity.HasOne(s => s.CinemaRoom)
                .WithMany(r => r.Seats)
                .HasForeignKey(s => s.CinemaRoomId);
            entity.HasOne(s => s.SeatType)
                .WithMany(t => t.Seats)
                .HasForeignKey(s => s.SeatTypeId);
        });

        modelBuilder.Entity<BookingSeat>(entity =>
        {
            entity.ToTable("booking_seat");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).HasColumnName("id");
            entity.Property(b => b.BookingId).HasColumnName("booking_id").IsRequired();
            entity.Property(b => b.ShowtimeId).HasColumnName("showtime_id").IsRequired();
            entity.Property(b => b.CinemaRoomId).HasColumnName("cinema_room_id").IsRequired();
            entity.Property(b => b.SeatId).HasColumnName("seat_id").IsRequired();
            entity.Property(b => b.SeatLabelSnapshot).HasColumnName("seat_label_snapshot").HasMaxLength(30).IsRequired();
            entity.Property(b => b.SeatTypeNameSnapshot).HasColumnName("seat_type_name_snapshot").HasMaxLength(100).IsRequired();
            entity.Property(b => b.SurchargeSnapshot).HasColumnName("surcharge_snapshot").HasColumnType("numeric").IsRequired();
            entity.Property(b => b.AllocationStatus).HasColumnName("allocation_status").HasMaxLength(16).IsRequired();
            entity.Property(b => b.ReleasedAt).HasColumnName("released_at");
            entity.HasOne(b => b.Showtime)
                .WithMany(s => s.BookingSeats)
                .HasForeignKey(b => b.ShowtimeId);
            entity.HasOne(b => b.Seat)
                .WithMany(s => s.BookingSeats)
                .HasForeignKey(b => b.SeatId);
        });
    }
}
