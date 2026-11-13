namespace CinemaTicketBooking.Api.Models;

public sealed class TicketCategory
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string EligibilityDescription { get; set; } = string.Empty;
    public long Version { get; set; }
    public ICollection<CinemaTicketPrice> CinemaTicketPrices { get; set; } = [];
}
