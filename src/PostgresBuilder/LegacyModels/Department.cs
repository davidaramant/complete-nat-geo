namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Department(
	int Id,
	int StartPageOffset,
	string DisplayName,
	string Summary,
	int SearchTime,
	int IssueId
);
