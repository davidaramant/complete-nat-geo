namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record DepartmentSubject(
	int Id,
	string DisplayName,
	int SearchTime,
	int StartPageOffset,
	int DepartmentId
);
