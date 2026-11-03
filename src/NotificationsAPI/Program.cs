using System.Globalization;
using NotificationsAPI.Application;
using NotificationsAPI.Integration;
using NotificationsAPI.Messaging;
using NotificationsAPI.Presentation;

var ptBr = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = ptBr;
CultureInfo.DefaultThreadCurrentUICulture = ptBr;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddLocalization();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<INotificationPresenter, ConsoleNotificationPresenter>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<NotificationEventDispatcher>();
builder.Services.AddSingleton<NotificationConsumerState>();
builder.Services.AddSingleton<EventMetrics>();
builder.Services.AddHostedService<EventMetricsWorker>();
builder.Services.AddHostedService<RabbitMqNotificationWorker>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "live", service = "NotificationsAPI" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live", service = "NotificationsAPI" }));
app.MapGet("/health/ready", (NotificationConsumerState state) => state.IsReady
    ? Results.Ok(new { status = "ready", service = "NotificationsAPI" })
    : Results.Json(new { status = "unavailable", service = "NotificationsAPI" }, statusCode: 503));

app.Run();

public partial class Program { }
