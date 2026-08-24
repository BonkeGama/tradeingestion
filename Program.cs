using Microsoft.EntityFrameworkCore;
using Serilog;
using TradeIngestionAssignment.Data;
using TradeIngestionAssignment.Options;
using TradeIngestionAssignment.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.Configure<TradeProcessingOptions>(
    builder.Configuration.GetSection(TradeProcessingOptions.SectionName));

builder.Services.AddDbContext<TradeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TradeDb")));

builder.Services.AddScoped<ITradeIngestionService, TradeIngestionService>();
builder.Services.AddScoped<IPortfolioSnapshotService, PortfolioSnapshotService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(dbContext, CancellationToken.None);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
