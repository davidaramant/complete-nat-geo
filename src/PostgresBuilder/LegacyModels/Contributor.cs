namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Contributor(
	int Id,
	string DisplayName,
	int SearchTime,
	int ArticleId,
	int DepartmentId,
	int MapId,
	int StartPageOffset
);
