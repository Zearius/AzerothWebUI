# AzerothWebUI

A self-hostable web front end for managing an AzerothCore (WotLK 3.3.5a, Playerbots-flavored)
private server — built for server owners who don't want to live in `docker attach` and the
in-game/console command line for routine tasks.

Runs as a single Docker container that drops into an existing AzerothCore docker-compose stack
(e.g. `wow-server-playerbots`, built for [Dad's MMO Lab](https://github.com/DadsMmoLab/dads-mmo-lab))
alongside it, sharing its network and config volume.

## Features

- **Public account registration** — self-service signup that creates a real `acore_auth`
  account with a correct SRP6 password verifier, no SOAP or console access required.
- **Player login** — players sign in with their existing game account credentials to view their
  characters.
- **Armory** — public character browsing (equipped gear, level, guild) and item lookup, including
  every drop source an item has (creature loot, fishing, gameobjects, skinning, disenchanting,
  and more).
- **Admin panel** — its own login, completely separate from WoW account credentials. Server
  status, account management (ban/unban/kick), and a searchable, metadata-aware editor for
  `worldserver.conf` and module config files that explains what each setting does and whether a
  change applies live or needs a restart.
- **AH Bot settings** — an editor for the Auction House Bot module's stocking and pricing rates
  and its disabled-item list, both of which live in the database rather than a config file.
- **Award Item** — search for an item and mail it to any character by name, whether or not
  they're currently online.

## Architecture

- **AzerothWebUI.Api** — ASP.NET Core minimal API. Owns all MySQL access and the AzerothCore SOAP
  client; the frontend never talks to the AzerothCore stack directly.
- **AzerothWebUI.Core** — class library with the domain logic, MySQL data access, SOAP client,
  and auth, independent of the ASP.NET hosting layer so it's unit-testable on its own.
- **AzerothWebUI.Core.Tests** — xUnit tests for `AzerothWebUI.Core`.
- **AzerothWebUI.Web** — React + TypeScript frontend (Vite), talks to the Api over HTTP.
- **AzerothWebUI.Tools** — a small console app for provisioning a GM SOAP account or an admin
  login without a running API — used by `setup.ps1` and available for manual setup too.

Registration, player login, and the armory only need MySQL access. The admin panel additionally
needs `SOAP.Enabled = 1` in `worldserver.conf` and a dedicated GM account for SOAP.

## Quick start (Windows + WSL2)

If your AzerothCore stack runs in WSL2, clone this repo on the Windows side and run the setup
script from PowerShell:

```powershell
git clone <this-repo-url>
cd AzerothWebUI
.\setup.ps1
```

This will:

1. Locate your AzerothCore docker-compose stack inside WSL.
2. Build the `azerothwebui` Docker image.
3. Add an `azerothwebui` service to that stack's `docker-compose.override.yml` with a randomly
   generated SOAP password and the right database connection strings filled in.
4. Provision a dedicated GM SOAP account and the app's own admin database.
5. Seed a first admin login and print its credentials — save them, they're shown only once.
6. Start the container.

Requires the .NET 10 SDK on the Windows host and Docker running inside the target WSL distro. If
it can't find your stack automatically, pass `-WslDistro <name>` and/or
`-WowServerPath <path-inside-wsl>`.

The script is safe to re-run. If `docker-compose.override.yml` already exists, it's never
rewritten — the script only adds the `azerothwebui` service to it if that's missing (backing the
file up first), and otherwise leaves it untouched, so any other customizations you've made to
that file are preserved. Pass `-Force` to regenerate the GM SOAP account and add another admin
login; this never touches `docker-compose.override.yml`'s content.

Once it's up, visit `http://localhost:8080` to register an account, `http://localhost:8080/login`
for player login, `http://localhost:8080/armory/characters` for the armory, or
`http://localhost:8080/admin/login` for the admin panel.

## Manual setup (any host)

1. Copy [`docker-compose.override.yml.example`](docker-compose.override.yml.example) into your
   AzerothCore stack's directory as `docker-compose.override.yml`, and fill in real connection
   strings and SOAP credentials.
2. Build the image: `docker build -t azerothwebui .`
3. Apply the admin database migration against the stack's database container:
   ```
   docker compose exec -T ac-database mysql -uroot -p<password> < AzerothWebUI.Api/Sql/001_create_admin_db.sql
   ```
4. Provision a dedicated GM SOAP account (never reuse a personal account) and a first admin login
   using `AzerothWebUI.Tools`:
   ```
   dotnet run --project AzerothWebUI.Tools -- create-gm-account WEBUISOAP <soap-password>
   dotnet run --project AzerothWebUI.Tools -- hash-admin-password <admin-password>
   ```
   Each command prints a ready-to-run SQL statement — run the first against the auth database and
   the second against the app's admin database, then fill the SOAP credentials into the override
   file from step 1.
5. `docker compose up -d azerothwebui`.

## Local development

Requires the .NET 10 SDK, Node 24+, and a running AzerothCore server reachable from this machine.

```
cp AzerothWebUI.Api/appsettings.Development.example.json AzerothWebUI.Api/appsettings.Development.json
# edit appsettings.Development.json with your connection strings and SOAP credentials
```

Run the API:

```
dotnet run --project AzerothWebUI.Api
```

Run the frontend:

```
cd AzerothWebUI.Web
npm install
npm run dev
```
