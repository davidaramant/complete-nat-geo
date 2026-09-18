using CompleteNatGeo.Data;
using Microsoft.EntityFrameworkCore;

namespace CompleteNatGeo.PostgresBuilder.Tests;

public sealed class CompleteNatGeoContextTests
{
	[Test]
	public async Task ShouldConfigureIssueAndPageEntitiesCorrectly()
	{
		var options = new DbContextOptionsBuilder<CompleteNatGeoContext>()
			.UseNpgsql("Host=localhost;Database=test;")
			.Options;

		await using var context = new CompleteNatGeoContext(options);
		var model = context.Model;

		var issueEntity = model.FindEntityType(typeof(Issue));
		await Assert.That(issueEntity).IsNotNull();
		await Assert.That(issueEntity.GetTableName()).IsEqualTo("issues");
		await Assert.That(issueEntity.GetSchema()).IsEqualTo("CompleteNatGeo");

		var issuePk = issueEntity.FindPrimaryKey();
		await Assert.That(issuePk).IsNotNull();
		await Assert.That(issuePk.Properties).Count().IsEqualTo(1);
		await Assert.That(issuePk.Properties).Contains(p => p.Name == "Id");
		await Assert.That(issueEntity.FindProperty(nameof(Issue.Id))?.GetColumnName()).IsEqualTo("id");
		await Assert
			.That(issueEntity.FindProperty(nameof(Issue.ReleaseDate))?.GetColumnName())
			.IsEqualTo("release_date");

		var pageEntity = model.FindEntityType(typeof(Page));
		await Assert.That(pageEntity).IsNotNull();
		await Assert.That(pageEntity.GetTableName()).IsEqualTo("pages");
		await Assert.That(pageEntity.GetSchema()).IsEqualTo("CompleteNatGeo");

		await Assert.That(pageEntity.GetForeignKeys()).Count().IsEqualTo(1);

		var fk = pageEntity.GetForeignKeys().Single();
		await Assert.That(fk.PrincipalEntityType.ClrType).IsEqualTo(typeof(Issue));
		await Assert.That(fk.Properties).Contains(p => p.Name == "IssueId");
		await Assert.That(fk.PrincipalKey.Properties).Contains(p => p.Name == "Id");

		var pagesNavigation = issueEntity.FindNavigation(nameof(Issue.Pages));
		await Assert.That(pagesNavigation).IsNotNull();
		await Assert.That(pagesNavigation.ForeignKey).IsEqualTo(fk);
	}

	[Test]
	public async Task ShouldGenerateValidSqlForDecadesQueryTranslation()
	{
		var options = new DbContextOptionsBuilder<CompleteNatGeoContext>()
			.UseNpgsql("Host=localhost;Database=test;")
			.Options;

		await using var context = new CompleteNatGeoContext(options);
		var query = context
			.Issues.GroupBy(i => i.ReleaseDate.Year / 10 * 10)
			.OrderByDescending(g => g.Key)
			.Select(g => new
			{
				Decade = g.Key,
				FileName = g.OrderBy(i => i.ReleaseDate)
					.Select(i => i.Pages.OrderBy(p => p.SortOrder).Select(p => p.FileName).FirstOrDefault())
					.FirstOrDefault(),
			});

		var sql = query.ToQueryString();
		await Assert.That(sql).IsNotNullOrWhiteSpace();
	}
}
