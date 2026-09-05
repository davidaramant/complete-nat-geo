using CompleteNatGeo.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CompleteNatGeo.PostgresBuilder.Tests;

public sealed class CompleteNatGeoContextTests
{
	[Fact]
	public void ModelConfiguration_ConfiguresIssueAndPageEntitiesCorrectly()
	{
		var options = new DbContextOptionsBuilder<CompleteNatGeoContext>()
			.UseNpgsql("Host=localhost;Database=test;")
			.Options;

		using var context = new CompleteNatGeoContext(options);
		var model = context.Model;

		var issueEntity = model.FindEntityType(typeof(Issue));
		issueEntity.ShouldNotBeNull();
		issueEntity.GetTableName().ShouldBe("issues");
		issueEntity.GetSchema().ShouldBe("CompleteNatGeo");

		var issuePk = issueEntity.FindPrimaryKey();
		issuePk.ShouldNotBeNull();
		issuePk.Properties.Select(p => p.Name).ShouldBe(["ReleaseDate"]);
		issueEntity.FindProperty(nameof(Issue.ReleaseDate))?.GetColumnName().ShouldBe("release_date");

		var pageEntity = model.FindEntityType(typeof(Page));
		pageEntity.ShouldNotBeNull();
		pageEntity.GetTableName().ShouldBe("pages");
		pageEntity.GetSchema().ShouldBe("CompleteNatGeo");

		var foreignKeys = pageEntity.GetForeignKeys().ToList();
		foreignKeys.Count.ShouldBe(1);

		var fk = foreignKeys.Single();
		fk.PrincipalEntityType.ClrType.ShouldBe(typeof(Issue));
		fk.Properties.Select(p => p.Name).ShouldBe(["IssueDate"]);
		fk.PrincipalKey.Properties.Select(p => p.Name).ShouldBe(["ReleaseDate"]);

		var pagesNavigation = issueEntity.FindNavigation(nameof(Issue.Pages));
		pagesNavigation.ShouldNotBeNull();
		pagesNavigation.ForeignKey.ShouldBe(fk);
	}
}
