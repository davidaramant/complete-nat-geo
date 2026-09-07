using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompleteNatGeo.Data.Configuration;

sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
	public void Configure(EntityTypeBuilder<Issue> builder)
	{
		builder.ToTable("issues", "CompleteNatGeo");

		builder.HasKey(issue => issue.Id);
		builder.Property(issue => issue.Id).HasColumnName("id");
		builder.Property(issue => issue.ReleaseDate).HasColumnName("release_date");
		builder
			.Property(issue => issue.Decade)
			.HasColumnName("decade")
			.HasComputedColumnSql("((EXTRACT(year FROM release_date)::integer / 10) * 10)", stored: true);

		builder.HasIndex(issue => issue.ReleaseDate);

		builder.HasMany(issue => issue.Pages).WithOne().HasForeignKey(page => page.IssueId);
	}
}
