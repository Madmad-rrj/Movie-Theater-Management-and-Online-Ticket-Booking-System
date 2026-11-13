namespace CinemaTicketBooking.Api.Models;

public sealed class Cinema
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public string Status { get; set; } = "active";
    public short MinimumTurnaroundMinutes { get; set; }
    public long Version { get; set; }
    public ICollection<CinemaRoom> Rooms { get; set; } = [];
    public ICollection<CinemaTicketPrice> TicketPrices { get; set; } = [];
}
