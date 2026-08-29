namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Map(
	int Id,
	int StartPageOffset,
	string DisplayName,
	string Summary,
	int SearchTime,
	string Size,
	string Scale,
	int ArticleId,
	int DepartmentId,
	int IssueId,
	string Filename
);
