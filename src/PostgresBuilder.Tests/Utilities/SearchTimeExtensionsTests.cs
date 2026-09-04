using CompleteNatGeo.PostgresBuilder.Utilities;
using Shouldly;

namespace CompleteNatGeo.PostgresBuilder.Tests.Utilities;

public class SearchTimeExtensionsTests
{
	[Theory]
	[InlineData(19700101, 1970, 1, 1)]
	[InlineData(19230928, 1923, 9, 28)]
	public void ShouldConvertSearchTimeToDateOnly(int searchTime, int year, int month, int day)
	{
		searchTime.ToDate().ShouldBe(new DateOnly(year, month, day));
	}
}
