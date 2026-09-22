namespace TravelPlanner.Api.Domain.Entities;

public sealed class PlacesImportState
{
    public Guid DestinationId { get; set; }
    public string? QuerySignature { get; set; }
    public DateTimeOffset? SucceededAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
}

public sealed class OverpassRequestState
{
    public int Id { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public DateTimeOffset? NextImportAt { get; set; }
    public DateTimeOffset? RetryAfter { get; set; }
    public DateTime BudgetDay { get; set; }
    public int Attempts { get; set; }
}
