namespace CompleteNatGeo.PostgresBuilder.Utilities;

public static class SearchTimeExtensions
{
	public static DateOnly ToDate(this int searchTime)
	{
		var year = searchTime / 10000;
		var month = (searchTime / 100) % 100;
		var day = searchTime % 100;
		return new DateOnly(year, month, day);
	}
}
