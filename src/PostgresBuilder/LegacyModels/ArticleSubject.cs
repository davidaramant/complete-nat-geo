namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record ArticleSubject(int Id, string DisplayName, int SearchTime, int StartPageOffset, int ArticleId);
