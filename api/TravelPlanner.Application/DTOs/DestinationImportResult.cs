namespace TravelPlanner.Application.DTOs;

public sealed record DestinationImportResult(bool IsSuccess, string? Slug, string? Error)
{
    public static DestinationImportResult Success(string slug) => new(true, slug, null);
    public static DestinationImportResult Failure(string error) => new(false, null, error);
}

public sealed record DestinationImportRequest(string? Qid);

public sealed record DestinationImportResponse(string Slug);
