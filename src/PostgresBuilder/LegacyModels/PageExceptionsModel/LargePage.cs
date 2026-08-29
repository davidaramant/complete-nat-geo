namespace CompleteNatGeo.PostgresBuilder.LegacyModels.PageExceptionsModel;

public sealed record LargePage(int Offset, int PageCount, float Ratio, string Filename);
