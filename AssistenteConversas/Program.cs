using AssistenteConversas.Components;
using AssistenteConversas.Data;
using AssistenteConversas.Services;
using AssistenteConversas.Models;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDirectory);
var databasePath = Path.Combine(dataDirectory, "leads.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.Configure<PoeOptions>(builder.Configuration.GetSection("AI"));
builder.Services.PostConfigure<PoeOptions>(options =>
{
    options.ApiKey = builder.Configuration["AI:ApiKey"] ?? Environment.GetEnvironmentVariable("POE_API_KEY");
});
builder.Services.AddHttpClient<PoeChatClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PoeOptions>>().Value;
    client.BaseAddress = new Uri($"{settings.BaseUrl.TrimEnd('/')}/");
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});
var usaPoe = string.Equals(builder.Configuration["AI:Provider"], "poe", StringComparison.OrdinalIgnoreCase);
builder.Services.AddScoped<MockConversationAnalyzer>();
builder.Services.AddScoped<PoeConversationAnalyzer>();
builder.Services.AddScoped<IConversationAnalyzer>(serviceProvider => usaPoe
    ? serviceProvider.GetRequiredService<PoeConversationAnalyzer>()
    : serviceProvider.GetRequiredService<MockConversationAnalyzer>());
builder.Services.AddScoped<MockChatAssistant>();
builder.Services.AddScoped<PoeChatAssistant>();
builder.Services.AddScoped<IChatAssistant>(serviceProvider => usaPoe
    ? serviceProvider.GetRequiredService<PoeChatAssistant>()
    : serviceProvider.GetRequiredService<MockChatAssistant>());
builder.Services.AddScoped<LeadChangeApplier>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddSingleton<ICsvExportService, CsvExportService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/leads/export", async (ILeadService leadService, ICsvExportService csvExportService) =>
{
    var leads = await leadService.SearchAsync(null, null, null, null);
    var content = csvExportService.Export(leads);
    var fileName = $"leads_{DateTime.Now:yyyy-MM-dd_HHmmss}.csv";
    return Results.File(content, "text/csv; charset=utf-8", fileName);
});

app.MapGet("/api/leads/{id:guid}/export", async (Guid id, ILeadService leadService, ICsvExportService csvExportService) =>
{
    var lead = await leadService.GetAsync(id);
    return lead is null
        ? Results.NotFound()
        : Results.File(csvExportService.Export([lead]), "text/csv; charset=utf-8", $"lead_{id:N}.csv");
});

app.Run();
