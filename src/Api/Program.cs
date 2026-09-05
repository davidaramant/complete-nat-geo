using CompleteNatGeo.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CompleteNatGeoContext>(options =>
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("CompleteNatGeo")
			?? throw new InvalidOperationException("Missing connection string 'CompleteNatGeo'.")
	)
);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapHealthChecks("/health");

app.MapGet(
		"/decades",
		async (CompleteNatGeoContext context) =>
		{
			var decades = await context
				.Issues.GroupBy(i => i.ReleaseDate.Year / 10 * 10)
				.OrderByDescending(g => g.Key)
				.Select(g => new
				{
					decade = g.Key,
					fileName = g.OrderBy(i => i.ReleaseDate)
						.Select(i => i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).FirstOrDefault())
						.FirstOrDefault(),
				})
				.ToListAsync();

			return decades;
		}
	)
	.WithName("GetDecades");

app.Run();
