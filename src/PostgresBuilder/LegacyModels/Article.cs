namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Article(
	int Id,
	int StartPageOffset,
	int PageCount,
	string DisplayName,
	string Summary,
	int SearchTime,
	int IssueId
);
