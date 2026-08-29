namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Map
{
	public required int Id { get; init; }
	public required int StartPageOffset { get; init; }
	public required string DisplayName { get; init; }
	public required string Summary { get; init; }
	public required int SearchTime { get; init; }
	public required string Size { get; init; }
	public required string Scale { get; init; }
	public required int ArticleId { get; init; }
	public required int DepartmentId { get; init; }
	public required int IssueId { get; init; }
	public required string Filename { get; init; }
}
