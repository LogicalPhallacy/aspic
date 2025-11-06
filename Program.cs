// Setup our service collection for interacting with Jellyfin
using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

var serviceCollection = new ServiceCollection();

// Add Http Client
serviceCollection.AddHttpClient("Default", c =>
    {
        c.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(
                "aspic-cli",
                "0.0.1"));

        c.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json, 1.0));
        c.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("*/*", 0.8));
    })
    .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8
    });

// Add Jellyfin SDK services.
serviceCollection.AddSingleton<JellyfinSdkSettings>();
serviceCollection.AddSingleton<IAuthenticationProvider, JellyfinAuthenticationProvider>();
serviceCollection.AddScoped<IRequestAdapter, JellyfinRequestAdapter>(s => new JellyfinRequestAdapter(
    s.GetRequiredService<IAuthenticationProvider>(),
    s.GetRequiredService<JellyfinSdkSettings>(),
    s.GetRequiredService<IHttpClientFactory>().CreateClient("Default")));
serviceCollection.AddScoped<JellyfinApiClient>();
serviceCollection.AddSingleton<BasicCommands>();

// Build the service provider
var serviceProvider = serviceCollection.BuildServiceProvider();
 // Initialize the sdk client settings. This only needs to happen once on startup.
        var sdkClientSettings = serviceProvider.GetRequiredService<JellyfinSdkSettings>();
        sdkClientSettings.Initialize(
            "aspic-cli",
            "0.0.1",
            Environment.MachineName,
            $"{Guid.NewGuid().ToString("N")}");
// Setup my command helpers


var command = new RootCommand("Aspic - A simple CLI for interacting with Jellyfin");
command.Add(serviceProvider.GetRequiredService<BasicCommands>().ConnectCommand());
command.Add(serviceProvider.GetRequiredService<BasicCommands>().ListCommand());
command.Add(serviceProvider.GetRequiredService<BasicCommands>().GetInfoCommand());
command.Add(serviceProvider.GetRequiredService<BasicCommands>().DownloadCommand());

var parsed = command.Parse(args);
if(parsed.Errors.Count == 0)
{
    return await parsed.InvokeAsync();
}
else
{
    foreach (var error in parsed.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }
    return 1;
}