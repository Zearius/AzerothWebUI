<#
.SYNOPSIS
    Sets up AzerothWebUI against a local wow-server-playerbots (or compatible AzerothCore)
    docker-compose stack running in WSL.

.DESCRIPTION
    Locates the target stack in WSL, clones/builds this repo as a sibling checkout, generates a
    docker-compose.override.yml with real credentials, provisions a GM SOAP account and the
    AzerothWebUI admin database, seeds the first admin login, and brings the container up.

    Safe to re-run: skips steps whose output already exists (GM account, admin login) unless
    -Force is passed. Never rewrites an existing docker-compose.override.yml's content -
    if the file exists but is missing the azerothwebui service, that service is appended
    (a timestamped .bak copy is taken first); if it already has the service, the file is
    left untouched entirely, regardless of -Force - see the Force parameter below.

.PARAMETER WslDistro
    The WSL distro name the target stack lives in. Auto-detected if omitted and only one distro
    is installed.

.PARAMETER WowServerPath
    WSL path to the wow-server-playerbots checkout (e.g. /home/dml/wow-server-playerbots).
    Auto-detected under the WSL user's home directory if omitted.

.PARAMETER Force
    Re-provision the GM SOAP account and reseed an admin login even if they already exist.
    Never rewrites an existing docker-compose.override.yml - that file may contain your own
    customizations (extra services, env vars) and this script only ever edits/creates the
    azerothwebui service within it, never the file as a whole.
#>
[CmdletBinding()]
param(
    [string]$WslDistro,
    [string]$WowServerPath,
    [switch]$Force,
    [string]$AdminUsername
)

# Not 'Stop': native commands invoked below (wsl.exe wrapping docker/git/curl) write routine
# progress to stderr (e.g. docker build's BuildKit output), and PowerShell 5.1 promotes every
# such line to a terminating ErrorRecord under $ErrorActionPreference = 'Stop', which would
# abort the script on success. Failures are instead detected explicitly via $LASTEXITCODE in
# each Invoke-Wsl*/Test-Wsl* helper below and surfaced with `throw`.
$ErrorActionPreference = 'Continue'

function Invoke-Wsl {
    param([Parameter(Mandatory)][string]$Command)
    # wsl.exe's stderr (e.g. docker build's BuildKit progress) is piped through 2>&1 at the
    # native-command level so PowerShell treats it as normal output rather than promoting each
    # line to a terminating ErrorRecord under $ErrorActionPreference = 'Stop'.
    $result = wsl -d $script:WslDistro -e bash -c $Command 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "WSL command failed (exit $LASTEXITCODE): $Command`n$result"
    }
    return $result
}

function Test-WslCommand {
    param([Parameter(Mandatory)][string]$Command)
    wsl -d $script:WslDistro -e bash -c $Command 2>&1 | Out-Null
    return $LASTEXITCODE -eq 0
}

function ConvertTo-WslPath {
    param([Parameter(Mandatory)][string]$WindowsPath)
    return (wsl -d $script:WslDistro -e wslpath -a ($WindowsPath -replace '\\', '\\')).Trim()
}

# Writes $ScriptBody to a temp file and runs it as a bash script inside WSL. Avoids nested
# quoting problems that come from embedding SQL/JSON containing quotes and semicolons inside a
# single `wsl -e bash -c "..."` string.
function Invoke-WslScript {
    param([Parameter(Mandatory)][string]$ScriptBody)

    $tempFile = [System.IO.Path]::GetTempFileName()
    try {
        # `set -e` so a failing command anywhere in the script (e.g. `curl -f` on a non-2xx
        # response) aborts immediately with a nonzero exit, instead of the script's exit code
        # being whatever the *last* line happens to return (a trailing `rm -f` on an existing
        # file always succeeds, which previously masked real failures earlier in the script).
        Set-Content -Path $tempFile -Value "set -e`n$ScriptBody" -NoNewline
        $wslTempPath = ConvertTo-WslPath $tempFile
        $result = wsl -d $script:WslDistro -e bash "$wslTempPath" 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "WSL script failed (exit $LASTEXITCODE):`n$ScriptBody`n---`n$result"
        }
        return $result
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

function Copy-ToWsl {
    param([Parameter(Mandatory)][string]$WindowsSourcePath, [Parameter(Mandatory)][string]$WslDestPath)
    $wslSourcePath = ConvertTo-WslPath $WindowsSourcePath
    Invoke-Wsl "cp '$wslSourcePath' '$WslDestPath'" | Out-Null
}

# Like Invoke-WslScript, but returns $true/$false based on exit code instead of throwing.
function Test-WslScript {
    param([Parameter(Mandatory)][string]$ScriptBody)

    $tempFile = [System.IO.Path]::GetTempFileName()
    try {
        Set-Content -Path $tempFile -Value $ScriptBody -NoNewline
        $wslTempPath = ConvertTo-WslPath $tempFile
        wsl -d $script:WslDistro -e bash "$wslTempPath" 2>&1 | Out-Null
        return $LASTEXITCODE -eq 0
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

function New-RandomPassword {
    param([int]$Length = 20)
    $chars = (48..57) + (65..90) + (97..122)
    -join (1..$Length | ForEach-Object { [char](Get-Random -InputObject $chars) })
}

Write-Host "=== AzerothWebUI setup ===" -ForegroundColor Cyan

# --- Step 1: locate WSL distro ---------------------------------------------------------------
if (-not $WslDistro) {
    $distros = (wsl -l -q) | Where-Object { $_ -and $_.Trim() } | ForEach-Object { $_.Trim() -replace "`0", '' }
    if ($distros.Count -eq 1) {
        $WslDistro = $distros[0]
    }
    elseif ($distros.Count -eq 0) {
        throw "No WSL distros found. Install WSL2 with your AzerothCore stack before running this script."
    }
    else {
        Write-Host "Multiple WSL distros found:" -ForegroundColor Yellow
        $distros | ForEach-Object { Write-Host "  $_" }
        $WslDistro = Read-Host "Which distro is wow-server-playerbots running in?"
    }
}
$script:WslDistro = $WslDistro
Write-Host "Using WSL distro: $WslDistro"

# --- Step 2: locate wow-server-playerbots checkout --------------------------------------------
if (-not $WowServerPath) {
    Write-Host "Searching for the wow-server-playerbots checkout..."
    $candidates = @('~/wow-server-playerbots', '~/wow-server', '~/azerothcore', '~/AzerothCore')
    foreach ($candidate in $candidates) {
        if (Test-WslCommand "test -f $candidate/docker-compose.yml -a -d $candidate/env/dist/etc") {
            $WowServerPath = Invoke-Wsl "cd $candidate && pwd"
            $WowServerPath = $WowServerPath.Trim()
            break
        }
    }

    if (-not $WowServerPath) {
        Write-Host "Not found in common locations, searching home directory (this may take a moment)..."
        $found = Invoke-Wsl "find ~ -maxdepth 4 -name docker-compose.yml -exec grep -l 'ac-worldserver' {} \; 2>/dev/null | head -1"
        if ($found -and $found.Trim()) {
            $WowServerPath = (Split-Path -Parent $found.Trim()) -replace '\\', '/'
        }
    }

    if (-not $WowServerPath) {
        $WowServerPath = Read-Host "Could not auto-detect. Enter the WSL path to your wow-server-playerbots checkout"
    }
}

$WowServerPath = $WowServerPath.TrimEnd('/')
Write-Host "Target stack: $WslDistro`:$WowServerPath"

$isValidStack = Test-WslCommand "grep -q 'ac-network' '$WowServerPath/docker-compose.yml' && grep -q 'ac-database' '$WowServerPath/docker-compose.yml' && grep -q 'ac-worldserver' '$WowServerPath/docker-compose.yml'"
if (-not $isValidStack) {
    throw "'$WowServerPath/docker-compose.yml' doesn't look like a wow-server-playerbots stack (missing ac-network/ac-database/ac-worldserver services). Pass -WowServerPath explicitly."
}
Write-Host "Confirmed: valid AzerothCore docker-compose stack." -ForegroundColor Green

# --- Step 3: locate this checkout and build the image ------------------------------------------
# setup.ps1 is expected to be run from within a cloned AzerothWebUI checkout (that's how the
# user got this script in the first place) - not a standalone downloaded script.
$repoRoot = Split-Path -Parent $PSCommandPath
if (-not ((Test-Path (Join-Path $repoRoot 'Dockerfile')) -and (Test-Path (Join-Path $repoRoot 'AzerothWebUI.slnx')))) {
    throw "setup.ps1 must be run from inside a full AzerothWebUI git checkout (Dockerfile/AzerothWebUI.slnx not found next to this script)."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK (dotnet) is required on Windows to provision the GM SOAP account and admin login. Install the .NET 10 SDK and re-run."
}

# Translate the Windows checkout path to its WSL equivalent so `docker build` runs inside WSL
# against the same checkout (building via /mnt/<drive> is slower than a native WSL path, but
# avoids maintaining a second clone in sync with this one).
$driveLetter = $repoRoot.Substring(0, 1).ToLower()
$restOfPath = $repoRoot.Substring(2) -replace '\\', '/'
$webUiWslPath = "/mnt/$driveLetter$restOfPath"

Write-Host "Building azerothwebui:latest image (this can take a few minutes)..."
Invoke-Wsl "cd '$webUiWslPath' && docker build -t azerothwebui:latest ." | Out-Null
Write-Host "Image built." -ForegroundColor Green

# --- Step 4: resolve DB root password -----------------------------------------------------------
$dbRootPassword = 'password'
if (Test-WslCommand "test -f '$WowServerPath/.env'") {
    $envValue = Invoke-Wsl "grep -E '^DOCKER_DB_ROOT_PASSWORD=' '$WowServerPath/.env' | cut -d= -f2-"
    if ($envValue -and $envValue.Trim()) {
        $dbRootPassword = $envValue.Trim()
    }
}

# --- Step 5: generate/update docker-compose.override.yml ----------------------------------------
# docker-compose.override.yml is a shared, hand-editable infrastructure file - AzerothCore's own
# base docker-compose.yml explicitly tells users to put their own customizations here (extra
# services, build targets, env vars for other containers like ac-worldserver). This script must
# never replace the whole file's existing content, even under -Force: doing so previously deleted
# a user's unrelated service definitions (playerbot bot-count env vars on ac-worldserver) because
# the file has a single top-level `services:` map and there is no way to "merge" by overwriting it
# wholesale. -Force here only ever means "regenerate the SOAP password and re-provision the GM
# account" - never "rewrite this file's contents." If the azerothwebui service block is missing
# from an existing file, it is appended (YAML map keys don't need to be contiguous, so appending
# a new entry under the existing top-level `services:` key is safe) rather than the script
# refusing or replacing anything.
$overridePath = "$WowServerPath/docker-compose.override.yml"
$overrideExists = Test-WslCommand "test -f '$overridePath'"
$hasAzerothWebUiService = $overrideExists -and (Test-WslCommand "grep -qE '^\s*azerothwebui:' '$overridePath'")

function Backup-OverrideFile {
    if (-not (Test-WslCommand "test -f '$overridePath'")) {
        return
    }
    $backupPath = "$overridePath.bak.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Invoke-Wsl "cp '$overridePath' '$backupPath'" | Out-Null
    Write-Host "Backed up existing docker-compose.override.yml to $backupPath" -ForegroundColor Yellow
}

$soapPassword = New-RandomPassword

if ($overrideExists -and $hasAzerothWebUiService) {
    $existingSoapPassword = Invoke-WslScript @"
grep 'AzerothCore__SoapPassword' '$overridePath' | grep -oE '"[^"]*"' | tail -1 | tr -d '"'
"@
    if ($existingSoapPassword -and $existingSoapPassword.Trim() -and $existingSoapPassword.Trim() -ne 'CHANGEME') {
        $soapPassword = $existingSoapPassword.Trim()
        if ($Force) {
            Write-Host "docker-compose.override.yml already has an azerothwebui service; -Force does not rewrite this shared file, so its SOAP password was left as-is. Edit AzerothCore__SoapPassword by hand in $overridePath if you want to rotate it (then re-run so the GM account gets recreated to match)." -ForegroundColor Yellow
        }
        else {
            Write-Host "docker-compose.override.yml already exists at $overridePath; leaving it as-is." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "Could not read the existing SOAP password from $overridePath; the summary below will show a freshly generated value that was NOT written anywhere - check the file directly if the account stops authenticating." -ForegroundColor Yellow
    }
}
else {
    # Extract just the `azerothwebui:` service block (2-space-indented lines) from the example -
    # not its `services:` key or trailing commentary - so it can be appended under an existing
    # file's own `services:` map without duplicating that key or disturbing anything else.
    $exampleLines = Get-Content (Join-Path $repoRoot 'docker-compose.override.yml.example')
    $serviceLines = $exampleLines | Where-Object { $_ -match '^  \S' -or $_ -match '^    ' }
    $serviceBlock = ($serviceLines -join "`n") `
        -replace '\$\{DOCKER_DB_ROOT_PASSWORD:-password\}', $dbRootPassword `
        -replace 'AzerothCore__SoapPassword: "CHANGEME[^"]*"', "AzerothCore__SoapPassword: `"$soapPassword`""

    if ($overrideExists) {
        Write-Host "Adding the azerothwebui service to the existing docker-compose.override.yml..."
        Backup-OverrideFile

        # Written to a temp file and appended via `cat >>` rather than embedding the block in a
        # bash command string - avoids nested-quoting problems with the YAML content's own
        # quotes/colons (see the WSL scripting pitfalls this project has hit before).
        $appendTempFile = [System.IO.Path]::GetTempFileName()
        Set-Content -Path $appendTempFile -Value "`n$serviceBlock" -NoNewline
        $remoteAppendPath = '/tmp/azerothwebui-service-block.yml'
        Copy-ToWsl -WindowsSourcePath $appendTempFile -WslDestPath $remoteAppendPath
        Remove-Item $appendTempFile

        Invoke-WslScript @"
cat '$remoteAppendPath' >> '$overridePath'
rm -f '$remoteAppendPath'
"@ | Out-Null
    }
    else {
        Write-Host "Generating docker-compose.override.yml..."
        $overrideContent = "services:`n$serviceBlock`n"
        $tempFile = [System.IO.Path]::GetTempFileName()
        Set-Content -Path $tempFile -Value $overrideContent -NoNewline
        Copy-ToWsl -WindowsSourcePath $tempFile -WslDestPath $overridePath
        Remove-Item $tempFile
    }
    Write-Host "Wrote the azerothwebui service to $overridePath" -ForegroundColor Green
}

# --- Step 6: ensure SOAP is enabled --------------------------------------------------------------
$worldserverConf = "$WowServerPath/env/dist/etc/worldserver.conf"
$soapEnabled = Invoke-Wsl "grep -E '^SOAP.Enabled' '$worldserverConf'"
if ($soapEnabled -notmatch '=\s*1') {
    Write-Host "Enabling SOAP.Enabled in worldserver.conf..." -ForegroundColor Yellow
    Invoke-Wsl "sed -i -E 's/^SOAP.Enabled\s*=.*/SOAP.Enabled = 1/' '$worldserverConf'" | Out-Null
    Write-Host "SOAP enabled. The worldserver container will need a restart for this to take effect." -ForegroundColor Yellow
    $script:NeedsWorldserverRestart = $true
}

# --- Step 7: provision admin DB + GM SOAP account -------------------------------------------------
Write-Host "Applying AzerothWebUI admin database migration..."
$adminSqlWsl = "$webUiWslPath/AzerothWebUI.Api/Sql/001_create_admin_db.sql"
Invoke-WslScript @"
cd '$WowServerPath'
docker compose exec -T ac-database mysql -uroot -p'$dbRootPassword' < '$adminSqlWsl'
"@ | Out-Null

$gmAccountExists = Test-WslScript @"
cd '$WowServerPath'
docker compose exec -T ac-database mysql -uroot -p'$dbRootPassword' -N -e "SELECT 1 FROM acore_auth.account WHERE username='WEBUISOAP'" | grep -q 1
"@
if ($gmAccountExists -and -not $Force) {
    Write-Host "GM SOAP account WEBUISOAP already exists; leaving it as-is." -ForegroundColor Yellow
}
else {
    Write-Host "Provisioning GM SOAP account (WEBUISOAP)..."
    if ($gmAccountExists) {
        Invoke-WslScript @"
cd '$WowServerPath'
docker compose exec -T ac-database mysql -uroot -p'$dbRootPassword' -e "DELETE FROM acore_auth.account_access WHERE id=(SELECT id FROM acore_auth.account WHERE username='WEBUISOAP'); DELETE FROM acore_auth.account WHERE username='WEBUISOAP';"
"@ | Out-Null
    }

    $gmSql = & dotnet run --project (Join-Path $repoRoot 'AzerothWebUI.Tools') -- create-gm-account WEBUISOAP $soapPassword
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate GM account SQL via AzerothWebUI.Tools."
    }
    $gmSqlFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $gmSqlFile -Value ($gmSql -join "`n") -NoNewline
    $remoteGmSqlPath = '/tmp/azerothwebui-gm-account.sql'
    Copy-ToWsl -WindowsSourcePath $gmSqlFile -WslDestPath $remoteGmSqlPath
    Remove-Item $gmSqlFile -ErrorAction SilentlyContinue

    Invoke-WslScript @"
cd '$WowServerPath'
docker compose exec -T ac-database mysql -uroot -p'$dbRootPassword' acore_auth < '$remoteGmSqlPath'
rm -f '$remoteGmSqlPath'
"@ | Out-Null
    Write-Host "GM SOAP account provisioned." -ForegroundColor Green
}

# --- Step 8: seed first AzerothWebUI admin login ---------------------------------------------------
$hasAdminUser = Test-WslScript @"
cd '$WowServerPath'
docker compose exec -T ac-database mysql -uroot -p'$dbRootPassword' -N -e "SELECT 1 FROM azerothwebui.AdminUsers LIMIT 1" | grep -q 1
"@

if ($hasAdminUser -and -not $Force) {
    Write-Host "An AzerothWebUI admin login already exists; skipping seeding (pass -Force to add another)." -ForegroundColor Yellow
}
else {
    if (-not $AdminUsername) {
        $AdminUsername = Read-Host "Enter a username for the AzerothWebUI admin login (default: admin)"
        if (-not $AdminUsername) { $AdminUsername = 'admin' }
    }
    $adminUsername = $AdminUsername
    $adminPassword = New-RandomPassword

    if ($hasAdminUser) {
        # -Force with an existing admin login: replace it rather than adding a second row, since
        # AdminUsers.Username is unique (case-insensitively) and a stale row with the same or a
        # colliding name would otherwise make the seed request fail with a 500.
        Write-Host "Removing existing AzerothWebUI admin login(s) before reseeding (-Force)..." -ForegroundColor Yellow
        Invoke-WslScript @"
cd '$WowServerPath'
docker compose exec -T ac-database mysql -uroot -p'$dbRootPassword' -e "DELETE FROM azerothwebui.AdminUsers;"
"@ | Out-Null
    }

    # Stop the real container first (if a prior run left one up) so the temporary Development-mode
    # seed container can bind the same port without a conflict; step 9 brings the real one back.
    Invoke-Wsl "docker rm -f azerothwebui" | Out-Null

    Write-Host "Starting azerothwebui temporarily in Development mode to seed the admin login..."
    Invoke-Wsl "cd '$WowServerPath' && docker compose run -d --name azerothwebui-seed -e ASPNETCORE_ENVIRONMENT=Development -p 8080:8080 azerothwebui" | Out-Null
    Start-Sleep -Seconds 5
    try {
        Invoke-WslScript @"
cat > /tmp/azerothwebui-seed-admin.json <<'PAYLOAD'
{"username":"$adminUsername","password":"$adminPassword"}
PAYLOAD
curl -s -f -X POST http://localhost:8080/api/dev/seed-admin -H 'Content-Type: application/json' --data-binary @/tmp/azerothwebui-seed-admin.json
rm -f /tmp/azerothwebui-seed-admin.json
"@ | Out-Null
        Write-Host "Admin login seeded." -ForegroundColor Green
    }
    finally {
        Invoke-Wsl "docker rm -f azerothwebui-seed" | Out-Null
    }
}

# --- Step 9: bring it up for real -------------------------------------------------------------------
Write-Host "Starting azerothwebui..."
Invoke-Wsl "cd '$WowServerPath' && docker compose up -d azerothwebui" | Out-Null

if ($script:NeedsWorldserverRestart) {
    Write-Host ""
    Write-Host "NOTE: SOAP was just enabled in worldserver.conf. Restart ac-worldserver for it to take effect:" -ForegroundColor Yellow
    Write-Host "  wsl -d $WslDistro -e bash -c `"cd $WowServerPath && docker compose restart ac-worldserver`""
}

Write-Host ""
Write-Host "=== Setup complete ===" -ForegroundColor Cyan
Write-Host "AzerothWebUI:  http://localhost:8080"
Write-Host "Admin login:   http://localhost:8080/admin/login"
if ($adminUsername -and $adminPassword) {
    Write-Host "  Username: $adminUsername"
    Write-Host "  Password: $adminPassword"
    Write-Host "  (shown only once - store it somewhere safe)" -ForegroundColor Yellow
}
else {
    Write-Host "  (using a previously seeded admin login)"
}
Write-Host ""
Write-Host "SOAP service account (also usable for other tooling):"
Write-Host "  Username: WEBUISOAP"
Write-Host "  Password: $soapPassword"
Write-Host "  (also saved in $overridePath)"
