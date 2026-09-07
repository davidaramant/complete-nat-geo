namespace Api;

public sealed record DecadeSummaryDto(int Decade, string CoverImageUrl);

public sealed record DecadeDetailDto(
	int Decade,
	int? PreviousDecade,
	int? NextDecade,
	IReadOnlyList<IssueSummaryDto> Issues
);

public sealed record IssueSummaryDto(int Id, DateOnly ReleaseDate, int PageCount, string CoverImageUrl);

public sealed record IssueDetailDto(
	int Id,
	DateOnly ReleaseDate,
	string CoverImageUrl,
	int Decade,
	int? PreviousIssueId,
	int? NextIssueId,
	IReadOnlyList<PageDto> Pages
);

public sealed record PageDto(int Id, int SortOrder, int? PageNumber, string ImageUrl);

public sealed record PageDetailDto(
	int Id,
	int IssueId,
	int Decade,
	int? PageNumber,
	DateOnly ReleaseDate,
	int? PreviousPageId,
	int? NextPageId,
	string ImageUrl
);
