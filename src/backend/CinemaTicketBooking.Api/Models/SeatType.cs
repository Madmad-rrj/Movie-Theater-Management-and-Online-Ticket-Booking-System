namespace CinemaTicketBooking.Api.Models;

public sealed class SeatType
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal SurchargeAmount { get; set; }
    public bool IsActive { get; set; }
    public long Version { get; set; }
    public ICollection<Seat> Seats { get; set; } = [];
}
