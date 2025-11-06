using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;

internal static class ProgramHelpers
{
    public static async Task<int> InitJellyfinClient(JellyfinSdkSettings sdkClientSettings, JellyfinApiClient jellyfinApiClient, bool quiet)
    {
        // Setup Jellyfin API client
        var jellyfinAddress = EnvironmentHelpers.GetJellyfinAddress();
        var jellyfinUser = EnvironmentHelpers.GetJellyfinUser();
        var jellyfinPassword = EnvironmentHelpers.GetJellyfinPassword();
        sdkClientSettings.SetServerUrl(jellyfinAddress);
        if (!quiet)
        {
            Console.WriteLine("Jellyfin client initialized, validating server connection...");   
        }

        try
        {
            // Get public system info to verify that the url points to a Jellyfin server.
            var systemInfo = await jellyfinApiClient.System.Info.Public.GetAsync()
                .ConfigureAwait(false);
            if (!quiet)
            {
                Console.WriteLine($"Connected to {jellyfinAddress}");
                Console.WriteLine($"Server Name: {systemInfo.ServerName}");
                Console.WriteLine($"Server Version: {systemInfo.Version}");
            }
        }
        catch (InvalidOperationException ex)
        {
            await Console.Error.WriteLineAsync("Invalid url").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return ex.HResult;
        }
        catch (SystemException ex)
        {
            await Console.Error.WriteLineAsync($"Error connecting to {jellyfinAddress}").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return ex.HResult;
        }
        if(!quiet)
            Console.WriteLine("Server validated. Checking authentication...");
        try
        {
            // Authenticate user.
            var authenticationResult = await jellyfinApiClient.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
                {
                    Username = jellyfinUser,
                    Pw = jellyfinPassword
                })
                .ConfigureAwait(false);
            sdkClientSettings.SetAccessToken(authenticationResult.AccessToken);
            var user = authenticationResult.User;
            if (!quiet)
            {
                Console.WriteLine("Authentication success.");
                Console.WriteLine($"Welcome to Jellyfin - {user.Name}");   
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync("Error authenticating.").ConfigureAwait(false);
            await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return ex.HResult;
        }

        return 0;
    }
}