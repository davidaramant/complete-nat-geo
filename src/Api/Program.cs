using Api;
using CompleteNatGeo.Data;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

Env.Load(Path.Combine(Repo.GetRoot(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CompleteNatGeoContext>(options =>
	options.UseNpgsql(GetPostgresConnectionString(builder.Configuration))
);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// TODO: Check this stuff when deployment becomes real
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
	});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();

	// Serve images from the local folder specified in ImagesPath
	var imagesPath = builder.Configuration["ImagesPath"];
	if (!string.IsNullOrWhiteSpace(imagesPath) && Directory.Exists(imagesPath))
	{
		app.UseStaticFiles(
			new StaticFileOptions { FileProvider = new PhysicalFileProvider(imagesPath), RequestPath = "/images" }
		);
	}
}

app.UseHttpsRedirection();
app.MapHealthChecks("/health");

app.MapGet(
		"/decades",
		async (CompleteNatGeoContext context, IConfiguration config) =>
		{
			// TODO: Pull this into an injectable helper
			// Retrieve configured base URL (e.g., "http://localhost:5000/images/" in dev)
			var imageBaseUrl = config["ImageBaseUrl"]?.TrimEnd('/') ?? "/images";

			var decades = await context
				.Issues.GroupBy(i => i.ReleaseDate.Year / 10 * 10)
				.OrderByDescending(g => g.Key)
				.Select(g => new
				{
					Decade = g.Key,
					FileName = g.OrderBy(i => i.ReleaseDate)
						.Select(i => i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).FirstOrDefault())
						.FirstOrDefault(),
					FirstIssueDate = g.OrderBy(i => i.ReleaseDate).Select(i => i.ReleaseDate).FirstOrDefault(),
				})
				.ToListAsync();

			return decades.Select(d => new
			{
				decade = d.Decade,
				imgUrl = $"{imageBaseUrl}/{d.Decade / 10}x/{d.FirstIssueDate:yyyyMMdd}/{d.FileName}.jpg",
			});
		}
	)
	.WithName("GetDecades");

app.Run();

static string GetPostgresConnectionString(IConfigurationManager config)
{
	var host = config["POSTGRES_HOST"] ?? throw new ArgumentException("Host");
	var user = config["POSTGRES_USER"] ?? throw new ArgumentException("User");
	var password = config["POSTGRES_PASSWORD"] ?? throw new ArgumentException("Password");
	var dbName = config["POSTGRES_DB"] ?? throw new ArgumentException("DB");
	var port = int.Parse(config["POSTGRES_PORT"] ?? throw new ArgumentException("Port"));

	return $"Host={host};Port={port};Database={dbName};Username={user};Password={password};";
}
