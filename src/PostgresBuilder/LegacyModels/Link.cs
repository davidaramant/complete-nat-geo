namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Link(
	int Id,
	int StartPageOffset,
	int ArticleId,
	int DepartmentId,
	int MapId,
	string DisplayName,
	int SearchTime
);
