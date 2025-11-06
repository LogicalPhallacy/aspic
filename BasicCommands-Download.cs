using System.CommandLine;
using Jellyfin.Sdk.Generated.Models;
using Spectre.Console;

public partial class BasicCommands
{
    public Command DownloadCommand()
    {
        Command downloadCommand = new("download", "Downloads content from the connected Jellyfin server");
        Argument<Guid> itemId = new("Id")
        {
            Description = "The ID of the item to download",
            Arity = ArgumentArity.ExactlyOne,
        };
        downloadCommand.Add(itemId);
        Argument<string> destination = new("destination")
        {
            Description = "The destination path for the downloaded item. If a folder is specified, the item will be downloaded into that folder. If the item is a collection, this will be treated as a foldername for the child items.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = (s) => Environment.CurrentDirectory
        };
        downloadCommand.Add(destination);
        Option<bool> force = new("--force", ["--force", "-f"])
        {
            Description = "Force download even if the file already exists",
            Arity = ArgumentArity.ZeroOrOne,
        };
        downloadCommand.Add(force);
        Option<int> throttle = new("--throttle", "-t")
        {
            Description = "The maximum number of concurrent downloads",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = (s) => Environment.ProcessorCount / 2
        };
        downloadCommand.Add(throttle);

        downloadCommand.SetAction(async (pr, cancelToken) =>
        {
            return await DownloadItem(pr.GetValue(itemId), pr.GetValue(destination), pr.GetValue(force), pr.GetValue(throttle));
        });
        return downloadCommand;
    }
    private async Task<int> DownloadItem(Guid itemId, string destination, bool force, int throttle)
    {
        var item = await runJellyfinCommand(() => apiCLient.Items[itemId].GetAsync());
        if (item is null)
        {
            Console.Error.WriteLine("Item not found.");
            return 404;
        }
        string destDir = string.Empty;
        string destFile = string.Empty;
        // Implementation for downloading an item goes here
        if (File.Exists(destination))
        {
            Console.Error.WriteLine($"Destination file {destination} already exists.");
            if (!force)
            {
                Console.Error.WriteLine("Use the --force option to overwrite.");
                return 1;
            }
        }
        if (Directory.Exists(destination))
        {
            Console.WriteLine($"Destination directory {destination} already exists.");
            destDir = sanitizeDirectoryName(destination);
        }
        else
        {
            if (Path.HasExtension(destination))
            {
                destDir = sanitizeDirectoryName(Path.GetDirectoryName(destination));
                destFile = sanitizeFileName(Path.GetFileName(destination));
            }
            else
            {
                destDir = destination;
            }
        }
        _ = Directory.CreateDirectory(destDir);

        await AnsiConsole.Progress().Columns([new SpinnerColumn(), new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn()])
            .StartAsync(async ctx =>
            {
                // Define tasks
                if ((item.ChildCount ?? 0) > 0)
                {
                    // Handle downloading child items if this is a collection
                    Console.WriteLine("This is a collection, creating folder and downloading child items...");
                    destDir = Path.Combine(destDir, sanitizeDirectoryName(item.Name));
                    _ = Directory.CreateDirectory(destDir);
                    SemaphoreSlim semaphore = new SemaphoreSlim(throttle);
                    var children = await apiCLient.Items.GetAsync(req => {
                        req.QueryParameters.ParentId = item.Id;
                        req.QueryParameters.Fields = [ItemFields.MediaSources, ItemFields.Path];
                        });
                    List<Task> downloadTasks = new List<Task>();
                    foreach (var child in children.Items ?? [])
                    {
                        if (!child.MediaSources.Any())
                        {
                            Console.Error.WriteLine($"No media sources available for {child.Name}.");
                            continue;
                        }
                        var childDownload = ctx.AddTask($"[green]{child.Name}[/]", false, maxValue: 100);
                        if (child.Path is not null)
                        {
                            destFile = sanitizeFileName(Path.GetFileName(child.Path));
                        }
                        downloadTasks.Add(downloadMediaItemWithProgress(childDownload, semaphore, child, Path.Combine(destDir, destFile)));
                    }
                    await Task.WhenAll(downloadTasks);
                }
                else
                {
                    var download = ctx.AddTask($"[green]Downloading {item.Name}[/]", maxValue: 100);
                    if (item.Path is not null)
                    {
                        destFile = sanitizeFileName(Path.GetFileName(item.Path));
                    }
                    await downloadMediaItemWithProgress(download, new SemaphoreSlim(1), item, Path.Combine(destDir, destFile));
                }
            });
        return 0;
    }
    
    private async Task downloadMediaItemWithProgress(ProgressTask task, SemaphoreSlim semaphore, BaseItemDto item, string destination)
    {
        try
        {
            task.Description = $"[green]Waiting to download {item.Name}[/]";
            await semaphore.WaitAsync();
            task.Description = $"[green]Starting download {item.Name}[/]";
            var mediaLength = item.MediaSources.FirstOrDefault()?.Size ?? 0;
            if (mediaLength == 0)
            {
                task.IsIndeterminate = true;
            }
            long progress = 0;
            using var mediaStream = await apiCLient.Items[item.Id.Value].File.GetAsync();
            using var fileStream = File.Create(destination);
            var buffer = new byte[81920];
            int bytesRead = 0;
            task.Description = $"[green]Downloading {item.Name}[/]";
            task.StartTask();
            Task trackerTask = task.IsIndeterminate ? Task.CompletedTask : Task.Run(async () =>
            {
                while (progress < mediaLength)
                {
                    task.Increment((double)progress / mediaLength * 100);
                    await Task.Delay(500);
                }
            });
            while ((bytesRead = await mediaStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                progress += bytesRead;
            }
        }
        finally
        {
            semaphore.Release();
            task.StopTask();
        }
    }

    private string sanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
    private string sanitizeDirectoryName(string directoryName)
    {
        foreach (char c in Path.GetInvalidPathChars())
        {
            directoryName = directoryName.Replace(c, '_');
        }
        return directoryName;
    }
}