# oyasumi - the osu! server implementation


##### Features
 * Score sumbission
 * Beatmap Leaderboard
 * Login
 * Chat
 * Spectators
 * Multiplayer
 * Data Caching (initial login around 120ms and second login around 0.1ms)
 * Commands
 

##### How can I contribute in this project?

 - You can ask access to collaborators
 - Or create pull request
 
#### Goals
 - Just for fun

# Setup
Before setup you need **dotnet**>5.0, on lower versions you won't be able compile oyasumi

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

If you want change port add to start oyasumi arguments `--urls=http://localhost:port`
