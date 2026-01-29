# ProjectTFDB

## Quick start

```powershell
dotnet build .\ProjectTFDB.sln -c Debug
dotnet run --project .\docs\Prototypes\Prototype1\Prototype1\ProjectTFDB.csproj -c Debug
```

## Automation

- Local: `.\scripts\dev.ps1` (build + run), `.\scripts\test.ps1` (tests)
- GitHub: CI builds on push/PR via `.github/workflows/ci.yml`

**Update #1:** Added CI workflow and local scripts.

## Executable location

After a build, the app executable is here:

```
docs\Prototypes\Prototype1\Prototype1\bin\Debug\net10.0-windows\ProjectTFDB.exe
```

For a Release build, use:

```
docs\Prototypes\Prototype1\Prototype1\bin\Release\net10.0-windows\ProjectTFDB.exe
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
- Steam client: `Steam` → `Settings` → `Account`, then click **Account Details**. Your 17‑digit SteamID64 is listed there.
- Steam profile URL: open your profile in a browser; if the URL ends with a 17‑digit number, that is your SteamID64.
- SteamID.io: paste your profile URL or custom vanity URL and it shows your SteamID64.

```
https://steamid.io/
```

Add the values in the app’s **Settings** tab (SteamID64, Steam Web API Key, Backpack.tf API Key), then click **Save**.

---

Made with help from OpenAI.
