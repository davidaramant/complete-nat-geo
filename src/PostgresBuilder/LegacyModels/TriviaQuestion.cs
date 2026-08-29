namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record TriviaQuestion(
	int Id,
	string NgId,
	string Prompt,
	string Answer1,
	string Answer2,
	string Answer3,
	string Answer4,
	string LinkText,
	string Difficulty,
	string Category,
	int SearchTime,
	int StartPageOffset
);
