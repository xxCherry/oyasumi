# oyasumi - the osu! server implementation

##### Features
 * Score sumbission
 * Beatmap Leaderboard
 * Login
 * Chat
 * Spectators
 * Multiplayer
 * Data Caching
 * Commands

##### How can I contribute in this project?
 - Create issues with bug reports
 
#### Goals
 - Just for fun

# Setup
Before setup you need **dotnet** > 5.0, on lower versions you won't be able to compile oyasumi

### Clone repository
```sh
$ git clone https://github.com/xxCherry/oyasumi --recurse-submodules
```

### Restore & Build all projects
```sh
$ dotnet restore . && dotnet build . -c Release
```

### Copy compiled oyasumi to your folder
```sh
$ cp -R oyasumi/bin/Release/net5.0 /any/path
```

### Start oyasumi and edit configuration file!
```sh
$ ./oyasumi
$ nano config.json
```

# Unique features

* Scheduled commands (multi-line commands): allows you to write argument in the next messages. (Idea by cmyui)
* Command presence filter: allows you to check if ***Presence*** matches the command conditions.

# FAQ

 * Q: How to start oyasumi on custom port?
 * A: `./oyasumi --urls=http://localhost:port`

 * Q: How to enable relax pp?
 * A: You need to edit osu!'s repository. Remove Mod.IsRanked check from PerformanceCalculator of mode you want (or from all modes) [Planned to use my own fork of osu! repository]
