# ProjectTFDB

## Quick start

```powershell
dotnet build .\ProjectTFDB.sln -c Debug
dotnet run --project .\ProjectTFDB\ProjectTFDB.csproj -c Debug
```

## Executable location

After a build, the app executable is here:

```
ProjectTFDB\bin\Debug\net10.0-windows\ProjectTFDB.exe
```

For a Release build, use:

```
ProjectTFDB\bin\Release\net10.0-windows\ProjectTFDB.exe
```

## Prototypes

Prototype files are stored under:

```
docs\Prototypes\
```

Current prototype snapshot:

```
docs\Prototypes\Prototype1\
```

Prototype executable location:

```
docs\Prototypes\Prototype1\Prototype1\bin\Debug\net10.0-windows\
```

## API keys and SteamID64

### Steam Web API Key
1) Sign in to Steam.
2) Visit the Steam Web API key page (login required):

```
https://steamcommunity.com/dev/apikey
```

3) Enter a domain name (can be your local machine name) and create a key.

### Backpack.tf API Key
1) Sign in to Backpack.tf.
2) Open the Developer Centre (Manage API Keys):

```
https://next.backpack.tf/account/api-access
```

3) Create a key and copy it.

### SteamID64
You can get your SteamID64 in any of these ways:
- Steam client: Account details shows your 17‑digit SteamID64.

```
https://flightsimulator.zendesk.com/hc/en-us/articles/360015953320-How-to-find-your-Steam-ID64-before-contacting-support
```

- Steam profile URL: if your profile URL is numeric, that 17‑digit number is your SteamID64.

Add the values in the app’s **Settings** tab (SteamID64, Steam Web API Key, Backpack.tf API Key), then click **Save**.
