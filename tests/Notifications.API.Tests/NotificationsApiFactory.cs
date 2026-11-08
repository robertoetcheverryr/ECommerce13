using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Notifications.API.Tests;

public sealed class NotificationsApiFactory : WebApplicationFactory<Program>
{
    public const string ErrorHandlingOverrideKey = "ErrorHandling:IncludeExceptionDetails";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(ConfigureErrorHandling);
    }

    private static void ConfigureErrorHandling(WebHostBuilderContext context, IConfigurationBuilder config)
    {
        var overrides = new List<KeyValuePair<string, string?>>();
        overrides.Add(new KeyValuePair<string, string?>(ErrorHandlingOverrideKey, "false"));
        config.AddInMemoryCollection(overrides);
    }
}
