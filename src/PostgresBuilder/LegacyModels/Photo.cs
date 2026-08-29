namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Photo(int Id, int? StartPageOffset, int? ArticleId, int? DepartmentId, int? MapId);
