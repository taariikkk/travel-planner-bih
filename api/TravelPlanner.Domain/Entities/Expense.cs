namespace TravelPlanner.Api.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal AmountKM { get; set; }
    public string Description { get; set; } = string.Empty;
    public Trip Trip { get; set; } = null!;
}
