var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "NotificationsAPI" }));

app.Run();

public partial class Program { }
