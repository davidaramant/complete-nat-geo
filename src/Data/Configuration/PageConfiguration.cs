using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompleteNatGeo.Data.Configuration;

sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
	public void Configure(EntityTypeBuilder<Page> builder)
	{
		builder.ToTable("pages", "CompleteNatGeo");

		builder.HasKey(page => page.Id);
		builder.Property(page => page.Id).HasColumnName("id");
		builder.Property(page => page.IssueId).HasColumnName("issue_id");
		builder.Property(page => page.SortOrder).HasColumnName("sort_order");
		builder.Property(page => page.PageNumber).HasColumnName("page_number");
		builder
			.Property(page => page.FileName)
			.HasColumnName("file_name")
			.IsUnicode(false)
			.HasMaxLength(64)
			.IsRequired();

		builder.HasIndex(page => new { page.IssueId, page.SortOrder });
	}
}
