using CompleteNatGeo.PostgresBuilder.Utilities;

namespace CompleteNatGeo.PostgresBuilder.Tests.Utilities;

public class SearchTimeExtensionsTests
{
	[Test]
	[Arguments(19700101, 1970, 1, 1)]
	[Arguments(19230928, 1923, 9, 28)]
	public async Task ShouldConvertSearchTimeToDateOnly(int searchTime, int year, int month, int day)
	{
		await Assert.That(searchTime.ToDate()).IsEqualTo(new DateOnly(year, month, day));
	}
}
