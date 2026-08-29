namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Link
{
	public required int Id { get; init; }
	public required int StartPageOffset { get; init; }
	public required int ArticleId { get; init; }
	public required int DepartmentId { get; init; }
	public required int MapId { get; init; }
	public required string DisplayName { get; init; }
	public required int SearchTime { get; init; }
}
