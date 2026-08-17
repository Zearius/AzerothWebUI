# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working in this repository.

## What this is

A self-hostable web front end for managing an AzerothCore (WotLK 3.3.5a, Playerbots-flavored)
private server. Built for server owners — starting with the [Dad's MMO Lab](https://github.com/DadsMmoLab/dads-mmo-lab)
Discord community — who currently have to `docker attach` into the worldserver console for
routine tasks like creating accounts. Long-term goal: ship as an optional container that drops
into that project's docker-compose stack, and possibly open-source it more broadly.

This repo is a fresh scaffold (see git history/README "Status" section for current progress —
don't trust this file for what's *implemented*, only for what's *decided* and *why*).

## Relationship to other repos

- **`e:\ThundercatsWoW`** — a separate repo of SQL scripts (item/gear scaling) for the same
  AzerothCore server this project manages. Not a dependency, just adjacent tooling by the same
  owner. Has its own CLAUDE.md with useful details about the local dev environment (see below).
- **`wow-server-playerbots`** (WSL, distro `dml-arch`, at `~/wow-server-playerbots`) — the
  actual AzerothCore-Playerbots server this UI targets in local dev. Docker-compose based (see
  Architecture section). Reachable from Windows via `\\wsl.localhost\dml-arch\...` for direct
  file access, or `wsl -d dml-arch -e bash -c "cd ~/wow-server-playerbots && <cmd>"` for
  commands.
- **Dad's MMO Lab** — the upstream project/Discord this is ultimately meant to plug into. No
  fork exists yet (owner needs to talk to the other developer first) — that's why this is a
  brand-new standalone repo for now rather than a subfolder/branch of that project.
- **`E:\SpellEditorMigrator`** — a prior tool by the same owner (ETLs spells from SpellEditor's
  DB into AzerothCore). Referenced as the closest precedent for "read from AzerothCore schema,
  present/migrate it" work, and as the naming-convention source for this repo's `.slnx` +
  multi-project layout.

## Owner background (relevant to how we should build this)

The owner is a professional C#/.NET API developer, not primarily a frontend or Node developer.
That's *why* the stack is ASP.NET Core on the backend rather than a Next.js/Python full-stack
app — see "Stack decision" below. Lean on idiomatic ASP.NET Core patterns; don't assume deep
Node/React fluency when explaining frontend pieces.

## Architecture

- **AzerothWebUI.Api** — ASP.NET Core minimal API (.NET 10). Owns *all* MySQL access and the
  SOAP client. The React app never talks to the AzerothCore stack directly — everything goes
  through this API.
- **AzerothWebUI.Core** — class library with domain logic, independent of ASP.NET hosting so
  it's unit-testable in isolation. Folders staked out (currently empty besides `.gitkeep`):
  - `Data/` — MySQL access (uses `MySqlConnector`, already added as a package reference)
  - `Domain/` — domain models/logic
  - `Soap/` — AzerothCore SOAP command client
  - `Auth/` — application-level auth (admin sessions — NOT the same as WoW account credentials)
- **AzerothWebUI.Core.Tests** — xUnit, references Core.
- **AzerothWebUI.Web** — React + TypeScript, scaffolded via Vite (`react-ts` template), plain
  Vite dev server — not Next.js (see decision below).

### Data flow by feature (planned)

1. **Public account registration** — writes directly to `acore_auth` with correct SRP6
   password-verifier generation. Pure MySQL, no SOAP, no console. This is the easiest/lowest-
   risk feature and a reasonable place to start actual implementation.
2. **Admin panel** (manage bots/players, ban, kick, disconnect) — via AzerothCore's built-in
   SOAP command API, which accepts the same commands as the in-game/console GM interface over
   HTTP/XML. Requires `SOAP.Enabled = 1` in `worldserver.conf` (disabled by default) and a GM
   SOAP account. Must sit behind this app's *own* admin authentication layer — never expose
   SOAP directly to end users, and never let public registration touch SOAP.
3. **Armory** (player/character lookup) — read-only queries against `acore_characters` (+
   `acore_world` for item/spell names). No SOAP needed.
4. **Server stats** — mix of live SOAP-derived data (players online, uptime) and, for anything
   historical, data we'd need to start logging ourselves — nothing stores history by default.
5. **Config editor** — structured, metadata-aware editing of game-related settings in
   `worldserver.conf` and per-module `.conf` files (`mod_ahbot.conf`, `playerbots.conf`, etc.).
   `authserver.conf` is explicitly **out of scope** — not a game-related config, not exposed in
   the UI. See "Config editor design" below.

## Config editor design

**Why this exists:** hand-editing `worldserver.conf` (~5000 lines) and module configs is the
kind of drudgery this whole project is meant to eliminate, same motivation as avoiding
`docker attach` for account creation.

**File format:** these are not INI in the strict sense — flat `key = value` lines (values
sometimes quoted strings, sometimes bare numbers/booleans) inside one implicit section, each
key preceded by a `#`-comment block containing a human-readable `Description:` and `Default:`.
That comment block is real metadata, not just prose — parse it and surface it in the UI
(description, default, valid values where enumerable) rather than building a plain key-value
text editor. This is a deliberate scope decision: a generic editor would be faster to build but
wouldn't actually solve the stated problem (people not understanding what a setting does).

**Source of truth is always the file on disk** — AzerothCore's `ConfigMgr` and each module's
config loader only ever read from the `.conf` file at startup/reload. There is no DB-backed
config path in the engine, so a DB-staging-layer design (store canonical values in our own DB,
sync out to files) was considered and rejected: it adds a consistency problem (file vs. DB
drift if anyone hand-edits the file) for no corresponding benefit, since the file has to be
written either way. Access the files through a small abstraction in `Core` (e.g.
`IConfigFileStore`) backed by a direct-filesystem implementation for local dev/same-host
deployments — interface exists so a remote (SSH/agent-based) implementation can be added later
*if* the API container ever doesn't share a filesystem with the AzerothCore stack, without
reworking domain logic. Don't build the remote implementation speculatively.

**Reload behavior after save — classify every key/file, don't just say "restart me":**
- AzerothCore's `World::LoadConfigSettings(bool reload)` (`src/server/game/World/World.cpp`)
  already re-reads `worldserver.conf` and re-applies values live, and is wired to the existing
  `reload config` command (`src/server/scripts/Commands/cs_reload.cpp`) — reachable over SOAP.
  Most `worldserver.conf` keys are hot-reloadable this way; no restart-orchestration needed for
  those, just issue SOAP `reload config` after writing the file.
- A minority of `worldserver.conf` keys are excluded from reload in that same function (e.g.
  `DataDir` — changing it via reload logs an error and keeps the old value) and need a real
  worldserver process restart.
- **Module configs are always treated as restart-required — deliberate, don't try to "fix"
  this.** Investigated all four modules in the local `wow-server-playerbots` stack
  (`mod-playerbots`, `mod-ah-bot`, `mod-talentbutton`, `mod-ale`) to check whether any hook the
  engine's generic `reload config` path (`ScriptMgr::OnBeforeConfigLoad`/`OnAfterConfigLoad`):
  - `mod-playerbots` (`playerbots.conf`) does *not* implement that hook at all. It has its own
    separate live-reload path — the in-game/console/SOAP GM command `.bot reload`
    (`modules/mod-playerbots/src/Bot/PlayerbotMgr.cpp:1035`) calls
    `sPlayerbotAIConfig.Initialize()` directly, no restart needed. But that's a distinct
    command from `reload config`, not something our generic save-flow trigger would hit.
  - `mod-ah-bot` and `mod-talentbutton` do implement `OnBeforeConfigLoad`/`OnAfterConfigLoad`,
    but only observed wired to world-startup hooks (`OnStartup`, `OnBeforeWorldInitialized`),
    not to a reload command — i.e. config is read once at boot.
  - `mod-ale` also implements the hook; body not fully inspected, but not worth finishing —
    see decision below.
  - **Decision: don't reimplement or special-case any of this per-module reload logic**
    (including `mod-playerbots`' `.bot reload`), even where a live-reload path exists. Each
    module's reload behavior is that module's own implementation detail, external to this app,
    and could change on a module update; hard-coding around it is exactly the kind of coupling
    this project shouldn't take on for a secondary convenience. Every module config key is
    always classified as "needs worldserver restart" in the UI, full stop, no per-module
    exceptions. Only `worldserver.conf`'s own `reload config` (owned by AzerothCore core, not a
    module) gets the hot-reload treatment.
- Save flow auto-detects the right action per key: fire SOAP `reload config` for hot-reloadable
  `worldserver.conf` keys, prompt/trigger a worldserver restart for everything else (excluded
  `worldserver.conf` keys and *all* module config keys).

## Key AzerothCore integration facts (learned this session, useful to re-derive quickly)

- The target docker-compose (`wow-server-playerbots/docker-compose.yml`) already runs
  everything on one `ac-network` bridge network with stable container names: `ac-database`
  (MySQL 8.4, databases `acore_auth`/`acore_world`/`acore_characters`/`acore_playerbots`),
  `ac-worldserver`, `ac-authserver`. A companion UI container just needs to join `ac-network`
  and can reach the DB/worldserver by container name — no service-discovery problem.
- SOAP is already port-mapped in that compose file: `${DOCKER_SOAP_EXTERNAL_PORT:-7878}:7878`
  on `ac-worldserver`. It is **disabled by default** — `SOAP.Enabled = 0` in
  `env/dist/etc/worldserver.conf` (binds to `127.0.0.1` by default via `SOAP.IP`). Must be
  flipped on and the bind address reconsidered depending on whether the UI container is on the
  same host.
- Recommended real-world integration path: add a new service block via
  `docker-compose.override.yml` in the target repo (that repo's own convention for
  user-added services — never edit its tracked `docker-compose.yml` directly).
- SOAP auth is a GM account's normal WoW username/password, submitted per-request — plan to
  provision a dedicated service/GM account for this rather than reusing a personal one.
- AzerothCore's SOAP protocol is simple XML-over-HTTP; a full SOAP client library is not
  required, a raw XML envelope + `HttpClient` is likely sufficient and simpler to reason about.

## Stack decisions (and why — don't relitigate without new information)

- **ASP.NET Core + React (Vite), not Next.js, not Blazor.** Owner is a working C#/.NET API
  developer; ASP.NET Core lets them move fast on the genuinely tricky parts (SRP6 hashing,
  SOAP XML plumbing, admin auth) in a language they already think fluently in. Next.js would
  stack "learning SOAP/SRP6" on top of "learning the framework" simultaneously. Blazor was
  considered (would let them stay 100% C#) but rejected because a plain React frontend is a
  more familiar shape for the self-hosted-OSS-tool audience this is meant to reach.
  - React chosen over Next.js specifically because v1 scope (forms + tables: registration,
    armory, admin panel, stats) doesn't need Next's SSR/routing machinery.
- **`.slnx` solution format**, matching the `SpellEditorMigrator` precedent.
- **MySqlConnector** (not `MySql.Data`/Oracle's connector) for MySQL access from Core.
- Secrets (connection strings, SOAP credentials) live in `AzerothWebUI.Api/appsettings.Development.json`,
  which is gitignored. A committed `appsettings.Development.example.json` documents the shape.
  Never put real credentials in `appsettings.json` (the committed base file) — it only holds
  empty placeholders under an `AzerothCore` section.

## Known non-issues (don't try to "fix" these)

- `dotnet build` emits `NU1903` (a known high-severity advisory on `Microsoft.OpenApi` 2.0.0,
  pulled in transitively by `Microsoft.AspNetCore.OpenApi` in the stock .NET 10 Web API
  template). This is upstream/template-wide as of this writing, not something introduced by
  this repo, and it's in build-time OpenAPI doc generation, not a runtime data path. Don't
  downgrade the target framework to chase it — check periodically for an updated
  `Microsoft.AspNetCore.OpenApi` package instead.
- `AzerothWebUI.Api/Program.cs` still has the default template's `/weatherforecast` sample
  endpoint. Harmless placeholder, delete it whenever the first real endpoint is added rather
  than as a standalone cleanup task.

## Working conventions

- No features are implemented yet as of this file's writing — check README.md's "Status"
  section and git log for current progress before assuming anything beyond the scaffold exists.
- When wiring up a new feature, prefer to build and test it against the actual local
  `wow-server-playerbots` WSL stack rather than mocking AzerothCore behavior — the SOAP/SQL
  quirks are exactly the risky part worth testing for real.
- Treat `Auth/` (this app's own admin authentication) and WoW account credentials as
  completely separate trust domains. Registration must never be able to reach SOAP or admin
  endpoints; admin endpoints must never accept a WoW account login as sufficient auth.
