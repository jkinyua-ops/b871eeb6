using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Nostra.DataLoad.Cin7APIClient;
using Nostra.DataLoad.UI;
using Nostra.DataLoad.UI.Data;
using Nostra.DataLoad.UI.DataService;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// SQL Server Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));


// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();


// Enhanced Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddCors(opt => {
    opt.AddDefaultPolicy( builder =>
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader());
});

builder.Services.Configure<ApiConfig>(builder.Configuration.GetSection("ApiConfig"));
builder.Services.Configure<SSOConfig>(builder.Configuration.GetSection("SSOConfig"));
var ssoConfig = builder.Configuration.GetSection("SSOConfig").Get<SSOConfig>();
builder.Services.AddSingleton<Serilog.ILogger>(provider => {
    var loggerConfiguration = new LoggerConfiguration()
        // Configure Serilog settings here
        .WriteTo.Console()
        .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
        //.WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)
        .CreateLogger();
    return loggerConfiguration;
});
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddDebug();
    loggingBuilder.AddConsole();
});
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie().AddOpenIdConnect("oidc", options =>
{
    options.Authority = ssoConfig.Authority;
    options.ClientId = ssoConfig.ClientId;
    options.ClientSecret = ssoConfig.ClientSecret;
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.UseTokenLifetime = false;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.TokenValidationParameters = new TokenValidationParameters { NameClaimType = "name" };

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.SignInScheme = "Identity.External";
    options.RemoteAuthenticationTimeout = TimeSpan.FromSeconds(50);
    options.UsePkce = false; // live does not support this yet
    options.CallbackPath = "/signin-oidc";
    options.Prompt = "login"; // login, consent
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.SaveTokens = true;
    options.NonceCookie.SameSite = SameSiteMode.None;
    //options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        NameClaimType = "email",
    };

    options.CorrelationCookie = new CookieBuilder
    {
        IsEssential = true,
        SameSite = SameSiteMode.None, // or SameSiteMode.Lax depending on your requirements
    };
    
    options.Events = new OpenIdConnectEvents
    {
        OnAccessDenied = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/");
            return Task.CompletedTask;
        }
    };
}).AddCookie("Identity.External");
  



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.Always
});
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.UseAuthentication();
app.UseAuthorization();
//await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);







// Auto-migration (Development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            context.Database.Migrate();
            app.Logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while migrating the database");
        }
    }
}


// Use CORS
app.UseCors("AllowSpecificOrigins");


// Health Checks Endpoint
app.MapHealthChecks("/health");


app.Run();
