namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record TriviaQuestion
{
	public required int Id { get; init; }
	public required string NgId { get; init; }
	public required string Prompt { get; init; }
	public required string Answer1 { get; init; }
	public required string Answer2 { get; init; }
	public required string Answer3 { get; init; }
	public required string Answer4 { get; init; }
	public required string LinkText { get; init; }
	public required string Difficulty { get; init; }
	public required string Category { get; init; }
	public required int SearchTime { get; init; }
	public required int StartPageOffset { get; init; }
}
