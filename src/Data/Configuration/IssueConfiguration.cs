using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompleteNatGeo.Data.Configuration;

sealed class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
	public void Configure(EntityTypeBuilder<Issue> builder)
	{
		builder.ToTable("issues", "CompleteNatGeo");

		builder.HasKey(issue => issue.ReleaseDate);
		builder.Property(issue => issue.ReleaseDate).HasColumnName("release_date");

		builder.HasMany(issue => issue.Pages)
			.WithOne()
			.HasForeignKey(page => page.IssueDate);
	}
}
