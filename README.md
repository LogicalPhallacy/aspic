# aspic

## What's This All About?

So you've got a Jellyfin server and you're tired of clicking around in the web UI just to download a bunch of media or check what's in your libraries? Me too. `aspic` is a casual little CLI tool that lets you interact with your Jellyfin server from the comfort of your terminal.

It's basically just a simple wrapper around the Jellyfin SDK that lets you:
- Connect to your Jellyfin server
- List your libraries and browse their contents
- Get detailed info about specific items
- Download media files (including entire collections!) with a fancy progress display

Its error handling borders on nonexistent, but its easier than doing it youself with curl and is fairly fast.

## Building This Thing

Prerequisites:
- .NET 10.0 SDK

Build it:
```bash
dotnet build
```

Run it:
```bash
dotnet run -- [command] [options]
```

Or if you want to publish it as a standalone binary:
```bash
dotnet publish -c Release
```

Then grab the executable from `bin/Release/net10.0/publish/` and put it wherever you want.

## Using It

First time you run any command (except the help), you'll be prompted for your Jellyfin server address, username, and password. Don't worry - it saves these credentials so you don't have to type them every time. They're stored in your AppData folder as `.aspicConfig`. Is this secure? Absolutely not, if you hae ideas about how to make it more secure, make an issue, or better yet a PR.

### Getting Help

The tool has built-in help for everything. Just use the `--help` flag:

```bash
aspic --help                    # See all available commands
aspic connect --help           # Help for the connect command
aspic list --help              # Help for the list command
aspic info --help              # Help for the info command
aspic download --help          # Help for the download command
```

### Quick Examples

Connect to your server:
```bash
aspic connect
```

Clear saved credentials and reconnect:
```bash
aspic connect --clear-credentials
```

List all your libraries:
```bash
aspic list --libraries
```

List contents of a specific library (you'll need the ID from the previous command):
```bash
aspic list <library-id>
```

Recursively list everything in a library (probably don't do this if its a big library, or do, I'm not a cop):
```bash
aspic list <library-id> --recurse
```

Get detailed info about an item (this will spit out json):
```bash
aspic info <item-id>
```

Download a single item to your working dir:
```bash
aspic download <item-id>
```

Download an item to a specific location:
```bash
aspic download <item-id> /path/to/destination
```

Download a collection with concurrency:
```bash
aspic download <collection-id> /path/to/folder --throttle 4
```

## Contributing

Make a PR. Ideally make sure it works first.

## A Few Notes

- The download command has a pretty slick progress display thanks to Spectre.Console
- When downloading collections, it'll create a folder and download all child items. Its not fully recursive yet, so you can't download an entire series at once yet.
- By default, concurrent downloads are limited to half your CPU core count, but you can adjust this with `--throttle`
- IDs are GUIDs - you can get them from the `list` commands
- All commands auto-connect if you're not already connected, so you don't need to run `connect` every time

That's it. Simple, straightforward, gets the job done. Enjoy!
