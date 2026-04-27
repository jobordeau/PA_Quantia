using Microsoft.EntityFrameworkCore;
using Quantia.Data;
using Quantia.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ISentimentRepository, EfSentimentRepository>();
builder.Services.AddScoped<SentimentService>();
builder.Services.AddScoped<PortfolioEquityService>();

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var inDocker = builder.Configuration.GetValue<bool>("RunningInContainer")
               || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

var cookieSecure = inDocker
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = cookieSecure;
});

builder.Services.AddAuthentication("QuantiaAuth")
    .AddCookie("QuantiaAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.SecurePolicy = cookieSecure;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = cookieSecure;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var mlApiBaseUrl = builder.Configuration["MlApi:BaseUrl"]
                   ?? "https://api-test-049u.onrender.com";

builder.Services.AddHttpClient("MLApi", client =>
{
    client.BaseAddress = new Uri(mlApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<TradeSuggestionService>(client =>
{
    client.BaseAddress = new Uri(mlApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICryptoPriceService, CryptoPriceService>(client =>
{
    client.BaseAddress = new Uri(mlApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<PortfolioPriceService>(client =>
{
    client.BaseAddress = new Uri(mlApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!inDocker)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseCookiePolicy();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.Run();
