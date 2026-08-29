namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Issue
{
	public required int Id { get; init; }
	public required string DisplayName { get; init; }
	public required int SearchTime { get; init; }
	public required int PageCount { get; init; }
	public required int NumberedPageOffset { get; init; }
	public required int NumberedPageCount { get; init; }
	public required int NumberedPageStartValue { get; init; }
	public required string PageExceptions { get; init; }
}
