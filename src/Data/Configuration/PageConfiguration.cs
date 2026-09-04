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
		builder.Property(page => page.IssueDate).HasColumnName("issue_date");
		builder.Property(page => page.SortOrder).HasColumnName("sort_order");
		builder.Property(page => page.PageNumber).HasColumnName("page_number");
		builder.Property(page => page.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
	}
}
