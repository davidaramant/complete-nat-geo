namespace CompleteNatGeo.PostgresBuilder.LegacyModels;

public sealed record PhotoSubject(int Id, string? DisplayName, int? SearchTime, int? StartPageOffset, int? PhotoId);
