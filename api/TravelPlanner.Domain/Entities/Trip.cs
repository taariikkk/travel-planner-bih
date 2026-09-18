namespace TravelPlanner.Api.Domain.Entities;

public sealed class Trip
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Travelers { get; set; }
    public User User { get; set; } = null!;
    public List<TripDay> Days { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
}
