using Microsoft.AspNetCore.RateLimiting;
using ProxyApiQualtech.Services.ControllerEntryData;
using ProxyApiQualtech.Services.FileWriter;
using ProxyApiQualtech.Services;
using ProxyApiQualtech.Services.BackGroundServices;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = WebApplication.CreateBuilder(args);

//lancer sur port spécifié
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5204); // http
}); 

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Host.UseWindowsService();
builder.Services.AddWindowsService();
builder.Services.AddHostedService<ServiceWorker>();
builder.Logging.AddEventLog(loggingBuilder =>
{
    loggingBuilder.SourceName = "GateWayApiLogs";
});
/*
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
builder.Services.AddHostedService<ServiceWorker>();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));*/


//rate limiter ajouté
builder.Services.AddRateLimiter(_ =>
{
    _.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 2000;
        options.Window = TimeSpan.FromSeconds(60);
    });
    _.RejectionStatusCode = 503;
});

//injections
builder.Services.AddScoped<IEntryDataInterpreter, EntryDataInterpreter>();
builder.Services.AddScoped<IFileWriter, LogFileWriter>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseRateLimiter();

app.MapGet("/", () => "").RequireRateLimiting("fixed");

app.MapControllers();

app.Run();
Console.WriteLine("Proxy started=>" + DateTime.Now);
Console.WriteLine("");