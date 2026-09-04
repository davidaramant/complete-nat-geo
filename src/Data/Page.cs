namespace CompleteNatGeo.Data;

public sealed class Page
{
	public int Id { get; set; }
	public required DateOnly IssueDate { get; set; }
	public required int SortOrder { get; set; }
	public required int? PageNumber { get; set; }
	public required string FileName { get; set; }
}
