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
builder.Services.AddHostedService<RabbitMqNotificationWorker>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "NotificationsAPI" }));

app.Run();

public partial class Program { }
