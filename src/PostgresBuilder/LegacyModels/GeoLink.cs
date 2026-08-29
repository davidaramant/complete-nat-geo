namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record GeoLink
{
	public required int Id { get; init; }
	public required int LocationId { get; init; }
	public required int ArticleId { get; init; }
	public required int? DepartmentId { get; init; }
	public required int? MapId { get; init; }
}
