using System.Reflection.Metadata;
using System.Text;
internal struct JellyfinCredentials
{
    public JellyfinCredentials(string address, string user, string password)
    {
        Address = address;
        User = user;
        Password = password;
    }
    public string Address { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
}
internal class EnvironmentHelpers
{
    // TODO: Store this in a marginally less stupid way
    public static string GetJellyfinAddress() => loadCreds().Address ?? promptForJellyfinAddress();
    public static string GetJellyfinUser() => loadCreds().User ?? promptForJellyfinUser();
    public static string GetJellyfinPassword() => loadCreds().Password ?? promptForJellyfinPassword();

    internal static JellyfinCredentials jellyfinCredentials;
    internal static bool loaded;
    internal static JellyfinCredentials loadCreds()
    {
        if (!loaded)
        {
            jellyfinCredentials = LoadJellyfinCredentials();
        }
        return jellyfinCredentials;
    }
    internal static readonly string AspicConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".aspicConfig");

    public static void ClearJellyfinCredentials()
    {
        if(File.Exists(AspicConfigPath))
        {
            File.Delete(AspicConfigPath);
        }
    }
    private static JellyfinCredentials LoadJellyfinCredentials()
    {
        if (!File.Exists(AspicConfigPath))
        {
            loaded = true;
            return new(null, null, null);
        }
        loaded = true;
        return System.Text.Json.JsonSerializer.Deserialize<JellyfinCredentials>(File.ReadAllText(AspicConfigPath));
    }
    private static void saveJellyfinCredentials()
    {
        File.WriteAllText(AspicConfigPath, System.Text.Json.JsonSerializer.Serialize(jellyfinCredentials));
    }
    private static string promptForJellyfinAddress()
    {
        Console.Write("Enter Jellyfin address (default: http://localhost:8096): ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? saveJellyfinAddress("http://localhost:8096") : saveJellyfinAddress(input);
    }
    private static string saveJellyfinAddress(string input)
    {
        jellyfinCredentials.Address = input;
        saveJellyfinCredentials();
        return input;
    }
    private static string promptForJellyfinUser()
    {
        Console.Write("Enter Jellyfin user (default: admin): ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? "admin" : saveJellyfinUser(input);
    }
    private static string saveJellyfinUser(string input)
    {
        jellyfinCredentials.User = input;
        saveJellyfinCredentials();
        return input;
    }
    private static string promptForJellyfinPassword()
    {
        Console.Write("Enter Jellyfin password: ");
        var input = readLineSecure();
        return string.IsNullOrWhiteSpace(input) ? "" : saveJellyfinPassword(input);
    }
    private static string readLineSecure()
    {
        var password = new StringBuilder();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        } while (key.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return password.ToString();
    }
    private static string saveJellyfinPassword(string input)
    {
        jellyfinCredentials.Password = input;
        saveJellyfinCredentials();
        return input;
    }
}