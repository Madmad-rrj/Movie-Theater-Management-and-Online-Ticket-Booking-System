using CinemaTicketBooking.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace CinemaTicketBooking.Api.Data;
internal static class SelectionMapping
{
    public static void ConfigureSelections(this ModelBuilder model)
    {
        var e = model.Entity<SelectionSession>();
        e.ToTable("selection_session"); e.HasKey(x => x.Id);
        e.Property(x => x.Id).ValueGeneratedNever();
        foreach (var p in e.Metadata.GetProperties())
            p.SetColumnName(System.Text.RegularExpressions.Regex.Replace(p.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
        e.Property(x => x.Version).IsConcurrencyToken();
        var s = model.Entity<SelectionSeat>();
        s.ToTable("selection_seat"); s.HasKey(x => new { x.SelectionId, x.SeatId });
        s.Property(x => x.SelectionId).HasColumnName("selection_id");
        s.Property(x => x.SeatId).HasColumnName("seat_id");
        s.Property(x => x.CinemaRoomId).HasColumnName("cinema_room_id");
        s.HasOne<SelectionSession>().WithMany().HasForeignKey(x => new { x.SelectionId, x.CinemaRoomId })
         .HasPrincipalKey(x => new { x.Id, x.CinemaRoomId }).OnDelete(DeleteBehavior.Restrict);
    }
}
