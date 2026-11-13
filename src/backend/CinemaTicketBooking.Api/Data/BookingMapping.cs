using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace CinemaTicketBooking.Api.Data;
internal static class BookingMapping
{
    public static void ConfigureBookings(this ModelBuilder model)
    {
        var booking = model.Entity<Booking>();
        booking.ToTable("booking"); booking.HasKey(x => x.Id);
        booking.Property(x => x.Version).IsConcurrencyToken();
        booking.HasIndex(x => x.SelectionSessionId).IsUnique();
        var category = model.Entity<BookingCategory>();
        category.ToTable("booking_category"); category.HasKey(x => new { x.BookingId, x.CategoryCode });
        category.HasOne<Booking>().WithMany(x => x.Categories).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        var seat = model.Entity<BookingSeat>();
        seat.ToTable("booking_seat"); seat.HasKey(x => x.Id);
        seat.HasOne<Booking>().WithMany(x => x.Seats).HasForeignKey(x => new { x.BookingId, x.ShowtimeId, x.CinemaRoomId })
            .HasPrincipalKey(x => new { x.Id, x.ShowtimeId, x.CinemaRoomId }).OnDelete(DeleteBehavior.Restrict);
        seat.HasIndex(x => new { x.ShowtimeId, x.SeatId }).IsUnique()
            .HasDatabaseName("ux_active_seat").HasFilter("allocation_status IN ('held','sold')");
        foreach (var entity in new[] { booking.Metadata, category.Metadata, seat.Metadata })
            foreach (var property in entity.GetProperties())
                property.SetColumnName(System.Text.RegularExpressions.Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
