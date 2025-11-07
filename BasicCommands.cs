using System.CommandLine;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;

public partial class BasicCommands(JellyfinApiClient apiCLient, JellyfinSdkSettings settings)
{
    bool isConnected;
    public Command ConnectCommand()
    {
        Command conn = new("connect", "Connects to a jellyfin server");
        Option<bool> clearCred = new("--clear-credentials", ["--clear-credentials", "-c"])
        {
            Description = "Clear saved credentials",
            Arity = ArgumentArity.ZeroOrOne,
        };
        conn.Add(clearCred);
        conn.SetAction(async (pr, cancelToken) =>
        {
            if (pr.GetValue(clearCred))
            {
                EnvironmentHelpers.ClearJellyfinCredentials();
            }
            return await ProgramHelpers.InitJellyfinClient(settings, apiCLient, false);
        });
        return conn;
    }
    public Command ListCommand()
    {
        Command listCommand = new("list", "Lists content on the connected Jellyfin server");
        Option<bool> libraries = new("Libraries", ["--libraries", "-l"])
        {
            Description = "Lists the libraries available to you",
            Arity = ArgumentArity.ZeroOrOne,
        };
        listCommand.Add(libraries);
        Option<bool> recurse = new("--recurse", ["--recurse", "-r"])
        {
            Description = "Recursively list child items",
            Arity = ArgumentArity.ZeroOrOne,
        };
        listCommand.Add(recurse);
        Argument<Guid> libraryId = new("Id")
        {
            Description = "The ID of the library or collection item to list contents for",
            Arity = ArgumentArity.ZeroOrOne,
        };
        listCommand.Add(libraryId);

        listCommand.SetAction(async (pr, cancelToken) =>
        {
            if (pr.GetValue(libraries))
            {
                return await ListLibraries();
            }
            // TODO: Other lists
            if (pr.GetValue(libraryId) != Guid.Empty)
            {
                var library = await runJellyfinCommand(() => apiCLient.Items[pr.GetValue(libraryId)].GetAsync());
                return await ListChildren(library, pr.GetValue(recurse));
            }
            return 0;
        });
        return listCommand;
    }
    public Command GetInfoCommand()
    {
        Command infoCommand = new("info", "Gets detailed information about a specific item");
        Argument<Guid> itemId = new("Id")
        {
            Description = "The ID of the item to get information for",
            Arity = ArgumentArity.ExactlyOne,
        };
        infoCommand.Add(itemId);

        infoCommand.SetAction(async (pr, cancelToken) =>
        {
            var item = await runJellyfinCommand(() => apiCLient.Items[pr.GetValue(itemId)].GetAsync());
            if (item == null)
            {
                Console.WriteLine("Item not found.");
                return 404;
            }
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(item, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        });

        return infoCommand;
    }
    private async Task Connect()
    {
        if (!isConnected)
        {
            var connect = await ProgramHelpers.InitJellyfinClient(settings, apiCLient, true);
            isConnected = connect == 0;
        }
    }

    private async Task<T> runJellyfinCommand<T>(Func<Task<T>> command)
    {
        try
        {
            await Connect();
            return await command();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return default;
        }
    }
    

    private async Task<int> ListLibraries()
    {

        var libraries = await runJellyfinCommand(() => apiCLient.UserViews.GetAsync());
        foreach (var lib in libraries?.Items ?? [])
        {
            // Write out the library name, id and type
            Console.WriteLine($"Library: {lib.Name} (ID: {lib.Id}) - {lib.CollectionType}");
        }
        return 0;
    }
    private async Task recursiveList(BaseItemDto parent, List<BaseItemDto> all, int tabs = 1)
    {
        Console.Write(new string('\t', tabs));
        Console.Write($"{parent.Name} (ID: {parent.Id}) - {parent.Type}");
        if (all.Any(c => c.ParentId == parent.Id))
        {
            Console.WriteLine(":");
            foreach (var child in all.Where(c => c.ParentId == parent.Id))
                await recursiveList(child, all, tabs + 1);
        } else
        {
            Console.WriteLine();
        }
    }
    private async Task<int> ListChildren(BaseItemDto parent, bool recurse, int tabs = 1)
    {
        Console.WriteLine($"{parent.Name} (ID: {parent.Id}) - {parent.Type}:");
        var children = await runJellyfinCommand(() => apiCLient.Items.GetAsync(req =>
        {
            req.QueryParameters.ParentId = parent.Id.Value;
            req.QueryParameters.Recursive = recurse;
            req.QueryParameters.Fields = [ItemFields.ParentId];
        }));
        var all = children?.Items ?? [];
        if (!recurse)
        {
            foreach (var child in all)
            {
                Console.Write(new string('\t', tabs));
                Console.WriteLine($"{child.Name} (ID: {child.Id}) - {child.Type}");
            }
        }
        else
        {
            foreach (var child in all.Where(c => c.ParentId == parent.Id) ?? [])
            {
                await recursiveList(child, all, tabs);   
            }
        }
        
        return 0;
    }
}