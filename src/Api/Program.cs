using Api;
using CompleteNatGeo.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

Env.Load(Path.Combine(Repo.GetRoot(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CompleteNatGeoContext>(options =>
	options.UseNpgsql(GetPostgresConnectionString(builder.Configuration))
);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IImageContext, ImageContext>();

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
	var imagesPath = builder.Configuration["IMAGES_PATH"];
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
		async (CompleteNatGeoContext context, IImageContext imageContext) =>
		{
			var decades = await context
				.Issues.GroupBy(i => i.Decade)
				.OrderByDescending(g => g.Key)
				.Select(g => new
				{
					Decade = g.Key,
					FileName = g.OrderBy(i => i.ReleaseDate)
						.Select(i => i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).First())
						.First(),
					FirstIssueDate = g.OrderBy(i => i.ReleaseDate).Select(i => i.ReleaseDate).FirstOrDefault(),
					NumIssues = g.Count(),
				})
				.ToListAsync();

			return decades.Select(d => new
			{
				decade = d.Decade,
				imgUrl = imageContext.GetUrl(d.FirstIssueDate, d.FileName),
				issues = d.NumIssues,
			});
		}
	)
	.WithName("GetDecades");

app.MapGet(
		"/decades/{decade:int}",
		async (CompleteNatGeoContext context, IImageContext imageContext, [FromRoute] int decade) =>
		{
			var issues = await context
				.Issues.Where(i => i.Decade == decade)
				.OrderBy(i => i.ReleaseDate)
				.Select(i => new
				{
					i.ReleaseDate,
					i.Id,
					FileName = i.Pages.Where(p => p.SortOrder == 0).Select(p => p.FileName).First(),
					NumPages = i.Pages.Count,
				})
				.ToListAsync();

			return issues.Select(i => new
			{
				releaseDate = i.ReleaseDate,
				id = i.Id,
				imgUrl = imageContext.GetUrl(i.ReleaseDate, i.FileName),
				pages = i.NumPages,
			});
		}
	)
	.WithName("GetDecade");

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
