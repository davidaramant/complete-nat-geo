namespace Data;

public class Page
{
	public int Id { get; set; }
	public DateOnly IssueDate { get; set; }
	public int SortOrder { get; set; }
	public int? PageNumber { get; set; }
	public required string FileName { get; set; }
}
