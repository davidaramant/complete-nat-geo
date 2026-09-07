namespace CompleteNatGeo.Data;

public sealed class Issue
{
	public int Id { get; set; }
	public required DateOnly ReleaseDate { get; set; }
	public List<Page> Pages { get; set; } = new();

	// Set by the database; see the configuration
	public int Decade { get; private set; }
}
