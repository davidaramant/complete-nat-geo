namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Article
{
	public required int Id { get; init; }
	public required int StartPageOffset { get; init; }
	public required int PageCount { get; init; }
	public required string DisplayName { get; init; }
	public required string Summary { get; init; }
	public required int SearchTime { get; init; }
	public required int IssueId { get; init; }
}
