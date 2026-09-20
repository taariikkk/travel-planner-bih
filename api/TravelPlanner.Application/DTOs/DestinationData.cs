namespace TravelPlanner.Application.DTOs;

public sealed record DestinationData(
    string Qid,
    string? NameBs,
    string? NameEn,
    double? Latitude,
    double? Longitude,
    string Type,
    double? ElevationM,
    long? Population,
    string? ImageName,
    string? BosnianWikipediaArticle,
    string? EnglishWikipediaArticle,
    DestinationDataRegion? Region);

public sealed record DestinationDataRegion(string? NameBs, string? NameEn);

public sealed record DestinationDataResult(bool IsSuccess, DestinationData? Data, string? Error)
{
    public static DestinationDataResult Success(DestinationData data) => new(true, data, null);
    public static DestinationDataResult Failure(string error) => new(false, null, error);
}

public sealed record DestinationDataSearchResult(bool IsSuccess, IReadOnlyList<DestinationData> Destinations, string? Error)
{
    public static DestinationDataSearchResult Success(IReadOnlyList<DestinationData> destinations) => new(true, destinations, null);
    public static DestinationDataSearchResult Failure(string error) => new(false, [], error);
}
