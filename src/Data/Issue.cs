namespace CompleteNatGeo.Data;

public sealed class Issue
{
	public required DateOnly ReleaseDate { get; set; }
	public int ReleaseOrder { get; set; }
	public List<Page> Pages { get; set; } = new();

	public int Decade => ReleaseDate.Year / 10 * 10;
}
