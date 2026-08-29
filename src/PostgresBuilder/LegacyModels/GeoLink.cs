namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record GeoLink(int Id, int? LocationId, int? ArticleId, int? DepartmentId, int? MapId);
