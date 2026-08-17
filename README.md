# AzerothWebUI

A self-hostable web front end for managing an AzerothCore (WotLK 3.3.5a, Playerbots-flavored)
private server — built for server owners who don't want to live in `docker attach` and the
in-game/console command line for routine tasks.

Aimed initially at [Dad's MMO Lab](https://github.com/DadsMmoLab/dads-mmo-lab) users, with an
eye toward eventually shipping as an optional container in that project's docker-compose stack.

## Planned features

1. **Public account registration** — self-service signup, writes directly to `acore_auth`
   with correct SRP6 password verifier generation. No SOAP/console involvement.
2. **Admin panel** — manage bots/players, ban, kick, disconnect, etc. Backed by AzerothCore's
   SOAP command API (`SOAP.Enabled` in `worldserver.conf`), sitting behind its own
   application-level admin auth (not reused WoW account credentials).
3. **Armory** — read-only character/item lookup against `acore_characters` + `acore_world`.
4. **Server stats** — online players, uptime, and whatever else is worth surfacing from SOAP
   `server info`-style commands plus data we choose to log over time.

## Architecture

- **AzerothWebUI.Api** — ASP.NET Core minimal API. Owns all MySQL access and the SOAP client;
  the React app never talks to the AzerothCore stack directly.
- **AzerothWebUI.Core** — class library with domain logic, MySQL data access (`Data/`), SOAP
  client (`Soap/`), and auth (`Auth/`), independent of the ASP.NET hosting layer so it's
  testable in isolation.
- **AzerothWebUI.Core.Tests** — xUnit tests for `AzerothWebUI.Core`.
- **AzerothWebUI.Web** — React + TypeScript frontend (Vite), consumes the Api over HTTP.

Registration and the armory/stats views only need MySQL. Admin actions additionally require
`SOAP.Enabled = 1` in the target server's `worldserver.conf` and a GM SOAP account.

## Local development

Requires .NET 10 SDK, Node 24+, and a running AzerothCore server (e.g. the
`wow-server-playerbots` docker-compose stack) reachable from this machine.

```
cp AzerothWebUI.Api/appsettings.Development.example.json AzerothWebUI.Api/appsettings.Development.json
# edit appsettings.Development.json with real connection strings / SOAP creds
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

## Status

Early scaffold. No features implemented yet.
