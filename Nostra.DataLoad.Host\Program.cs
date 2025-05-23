using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nostra.DataLoad.AutotaskAPIClient;
using Nostra.DataLoad.Cin7APIClient;
using RestSharp.Authenticators;
using Serilog;
using Nostra.DataLoad.Core;
using Nostra.DataLoad.Core.Servcices;
using Nostra.DataLoad.Domain.InfustructureLayer.Context;
using Nostra.DataLoad.Domain.InfustructureLayer.Repository;
using Orleans.Runtime;
using Nostra.DataLoad.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Nostra.DataLoad.Host;
using System;
using Microsoft.Extensions.Options;
//using Serilog.Sinks.C

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
builder.Services.AddDbContextFactory<ApplicationDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSqlConnection"));
    
}, ServiceLifetime.Transient);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<AutotaskSettings>(builder.Configuration.GetSection("AutotaskSettings"));
builder.Services.Configure<Cin7Settings>(builder.Configuration.GetSection("Cin7Settings"));

#region AppServices


#endregion

builder.Services.AddTransient<ICompanyAppService, CompanyAppService>();
builder.Services.AddTransient<ICountryAppService, CountryAppService>();
builder.Services.AddTransient<ITicketChargeAppService, TicketChargeAppService>();
builder.Services.AddTransient<IProductAppService, ProductAppService>();
builder.Services.AddTransient<IContactAppService, ContactAppService>();
builder.Services.AddTransient<ITicketAppService, TicketAppService>();
builder.Services.AddTransient<IAutotaskApiClient, AutotaskApiClient>();
builder.Services.AddTransient<ICin7ApiClient, Cin7ApiClient>();
//builder.Services.AddTransient<ICin7AppService, Cin7AppService>();
builder.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton<Serilog.ILogger>(provider => {
    var loggerConfiguration = new LoggerConfiguration()
        // Configure Serilog settings here
        .WriteTo.Console()
        .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
        //.WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces)
        .CreateLogger();
    return loggerConfiguration;
});




var taskQueueConnection = builder.Configuration.GetConnectionString("TaskQueueConnection");
builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("taskQueueConnection")));
builder.Services.AddHangfireServer();
builder.Services.AddAutoMapper(typeof(MappingProfile));
var app = builder.Build();


// Configure recurring jobs
//hangfireJobs.ConfigureRecurringJobs();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapHangfireDashboard(
                    "/hangfire",
                    new DashboardOptions
                    {
                        AppPath = "/hangfire", Authorization = new[] { new HangfireAuthorizationFilter() }
                    }
                ) ;
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//var serviceProvider = builder.Services.BuildServiceProvider();
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var atSettings = scope.ServiceProvider.GetRequiredService<IOptions<AutotaskSettings>>();
        var cin7Settings = scope.ServiceProvider.GetRequiredService<IOptions<AutotaskSettings>>();
        Log.Warning(System.Text.Json.JsonSerializer.Serialize(atSettings));
        Log.Warning(System.Text.Json.JsonSerializer.Serialize(cin7Settings));
        Log.Warning(builder.Configuration.GetConnectionString("DefaultSqlConnection"));

        db.Database.Migrate();
    }
    //var serviceProvider = builder.Services.BuildServiceProvider();
    //var hangfireJobs = new HangfireJobs();
    //hangfireJobs.ConfigureRecurringJobs(serviceProvider.GetRequiredService<ITicketChargeAppService>());
    RecurringJob.AddOrUpdate<ITicketChargeAppService>("Ticket Charge App Service", x => x.FetchAndSave(), "*/15 * * * *");
    RecurringJob.AddOrUpdate<ITicketAppService>("Sync to cin7", x => x.SyncToCin7Job(), "32 7-19/1 */1 * *");

}
catch (Exception ex)
{
    Log.Error("Config Error",ex.Message+ ex.InnerException+ex.StackTrace,ex);
}

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
