namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record Issue(
	int Id,
	string DisplayName,
	int SearchTime,
	int PageCount,
	int NumberedPageOffset,
	int NumberedPageCount,
	int NumberedPageStartValue,
	string PageExceptions
);
