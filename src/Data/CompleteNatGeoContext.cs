using CompleteNatGeo.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CompleteNatGeo.Data;

public sealed class CompleteNatGeoContext : DbContext
{
	private readonly string? _connectionString;

	public CompleteNatGeoContext() { }

	public CompleteNatGeoContext(DbContextOptions<CompleteNatGeoContext> options)
		: base(options) { }

	public CompleteNatGeoContext(string connectionString)
	{
		_connectionString = connectionString;
	}

	public DbSet<Page> Pages => Set<Page>();

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured && !string.IsNullOrWhiteSpace(_connectionString))
		{
			optionsBuilder.UseNpgsql(_connectionString);
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema("CompleteNatGeo");
		modelBuilder.ApplyConfiguration(new PageConfiguration());
	}
}
