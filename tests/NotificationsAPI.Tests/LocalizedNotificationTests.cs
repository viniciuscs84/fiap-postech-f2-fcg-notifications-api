using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NotificationsAPI.Presentation;

namespace NotificationsAPI.Tests;

public sealed class LocalizedNotificationTests
{
    [Fact]
    public void WelcomeMessageUsesPtBrResource()
    {
        using var culture = new CultureScope("pt-BR");
        using var services = new ServiceCollection()
            .AddLogging()
            .AddLocalization()
            .BuildServiceProvider();
        var localizer = services.GetRequiredService<IStringLocalizer<NotificationMessages>>();

        var message = localizer["WelcomeEmail", "pessoa@example.com"].Value;

        Assert.Equal("E-mail simulado para pessoa@example.com: bem-vindo à FIAP Cloud Games!", message);
    }

    [Fact]
    public void PurchaseMessageUsesPtBrResourceAndContainsNoCredentialData()
    {
        using var culture = new CultureScope("pt-BR");
        using var services = new ServiceCollection()
            .AddLogging()
            .AddLocalization()
            .BuildServiceProvider();
        var localizer = services.GetRequiredService<IStringLocalizer<NotificationMessages>>();
        var gameId = Guid.NewGuid();

        var message = localizer["PurchaseApprovedEmail", "pessoa@example.com", gameId].Value;

        Assert.Equal($"E-mail simulado para pessoa@example.com: a compra do jogo {gameId} foi confirmada.", message);
        Assert.DoesNotContain("password", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
