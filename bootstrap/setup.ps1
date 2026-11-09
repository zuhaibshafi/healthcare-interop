<#
.SYNOPSIS
  One-shot bootstrap for the healthcare-interoperability assessment stack on a fresh Windows machine.
  No Docker, no WSL, no administrator rights required. Everything is installed as portable binaries.

.DESCRIPTION
  Creates <Root> (default: the parent folder of this package, e.g. C:\git) with:
    tools\                 JDK 21, Maven 3.9, PostgreSQL 16, Redis 8, Ruby 3.3 + MSYS2, HL7 validator wrapper
    hapi-fhir-jpaserver-starter\, eCRNow\, us-core-test-kit\, uds-plus-test-kit\   (git clones, pinned commits, patched, built)
    hapi-runtime\, ecrnow-runtime\, inferno-runtime\                              (configs + working dirs)
  and builds the .NET integration layer in <package>\dotnet.

  Prerequisites that must already be on the machine (checked, installed with winget if missing):
    git, .NET SDK 9.x, Python 3.10+ (for the demo/driver scripts), Internet access.

  Idempotent: completed steps are skipped on re-run. Log: <package>\logs\setup.log

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File C:\git\interop-assessment\bootstrap\setup.ps1
  powershell -ExecutionPolicy Bypass -File ...\setup.ps1 -SkipInferno      # faster: HAPI + eCRNow + .NET only
#>
[CmdletBinding()]
param(
  [string]$Root,
  [switch]$SkipInferno,
  [switch]$SkipDotnet,
  [int]$PgInitPort = 5432,         # port used only while initialising the database (change if 5432 is busy during setup)
  [string]$RubyHomeOverride        # reuse a Ruby 3.3 + MSYS2 installation that already exists at this path
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$Package = Split-Path -Parent $PSScriptRoot           # ...\interop-assessment
if (-not $Root) { $Root = Split-Path -Parent $Package } # ...\  (e.g. C:\git)
$Root  = $Root.TrimEnd('\')
$RootFwd = $Root -replace '\\', '/'
$Tools = Join-Path $Root 'tools'
$Dl    = Join-Path $Tools 'downloads'
$Logs  = Join-Path $Package 'logs'
New-Item -ItemType Directory -Force -Path $Tools, $Dl, $Logs | Out-Null
$LogFile = Join-Path $Logs 'setup.log'
Start-Transcript -Path $LogFile -Append | Out-Null

function Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Ok($msg)   { Write-Host "    [OK] $msg" -ForegroundColor Green }
function Have($cmd) { return [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }
function Download($url, $dest) {
  if (Test-Path $dest) { Ok "already downloaded: $(Split-Path -Leaf $dest)"; return }
  Write-Host "    downloading $url"
  $tmp = "$dest.part"
  Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing
  Move-Item $tmp $dest -Force
}
function Expand($zip, $dest) {
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $dest)
}
function Run($file, $argList, $cwd, $envVars) {   # ($args is reserved in PowerShell)
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $file; $psi.Arguments = $argList; $psi.WorkingDirectory = $cwd
  # stdout/stderr are captured and echoed: child tools such as initdb fail when they inherit a redirected console handle
  $psi.UseShellExecute = $false; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
  if ($envVars) { foreach ($k in $envVars.Keys) { $psi.EnvironmentVariables[$k] = $envVars[$k] } }
  $p = [System.Diagnostics.Process]::Start($psi)
  $outTask = $p.StandardOutput.ReadToEndAsync(); $errTask = $p.StandardError.ReadToEndAsync()
  $p.WaitForExit()
  $out = $outTask.Result; $err = $errTask.Result
  if ($out) { Write-Host ($out.TrimEnd()) }
  if ($err) { Write-Host ($err.TrimEnd()) }
  if ($p.ExitCode -ne 0) { throw "command failed ($($p.ExitCode)): $file $argList" }
}

# ------------------------------------------------------------------------------------------------
Step "0. Checking machine prerequisites (git, .NET SDK 9, Python 3)"
if (-not (Have git))    { winget install --id Git.Git -e --accept-source-agreements --accept-package-agreements --scope user; $env:PATH = "$env:PATH;$env:LOCALAPPDATA\Programs\Git\cmd" }
if (-not (Have dotnet) -or -not ((dotnet --list-sdks) -match '^9\.')) { Write-Host "    installing .NET SDK 9 via winget (user scope)"; winget install --id Microsoft.DotNet.SDK.9 -e --accept-source-agreements --accept-package-agreements }
if (-not (Have python)) { winget install --id Python.Python.3.12 -e --accept-source-agreements --accept-package-agreements --scope user }
Ok "git $(git --version | Select-Object -First 1)"; Ok "dotnet $(dotnet --version)"; Ok "$(python --version)"

# ------------------------------------------------------------------------------------------------
Step "1. Portable toolchain -> $Tools"
# JDK 21 (Temurin)
if (-not (Get-ChildItem $Tools -Directory -Filter 'jdk-21*' -ErrorAction SilentlyContinue)) {
  Download 'https://api.adoptium.net/v3/binary/latest/21/ga/windows/x64/jdk/hotspot/normal/eclipse?project=jdk' "$Dl\jdk21.zip"
  Expand "$Dl\jdk21.zip" $Tools
}
$JavaHome = (Get-ChildItem $Tools -Directory -Filter 'jdk-21*' | Select-Object -First 1).FullName; Ok "JDK: $JavaHome"
# Maven
if (-not (Test-Path "$Tools\apache-maven-3.9.9")) { Download 'https://archive.apache.org/dist/maven/maven-3/3.9.9/binaries/apache-maven-3.9.9-bin.zip' "$Dl\maven.zip"; Expand "$Dl\maven.zip" $Tools }
$MavenHome = "$Tools\apache-maven-3.9.9"; Ok "Maven: $MavenHome"
# PostgreSQL 16 portable binaries
if (-not (Test-Path "$Tools\pgsql\bin\pg_ctl.exe")) {
  Download 'https://get.enterprisedb.com/postgresql/postgresql-16.9-1-windows-x64-binaries.zip' "$Dl\postgres.zip"
  Expand "$Dl\postgres.zip" "$Tools\pg-tmp"; Move-Item "$Tools\pg-tmp\pgsql" "$Tools\pgsql"; Remove-Item "$Tools\pg-tmp" -Recurse -Force
}
Ok "PostgreSQL: $Tools\pgsql"
# Redis 8 (Windows build; Sidekiq needs Redis >= 6.2)
if (-not (Get-ChildItem "$Tools\redis8" -Recurse -Filter 'redis-server.exe' -ErrorAction SilentlyContinue)) {
  Download 'https://github.com/redis-windows/redis-windows/releases/download/8.10.3/Redis-8.10.3-Windows-x64-msys2.zip' "$Dl\redis8.zip"
  Expand "$Dl\redis8.zip" "$Tools\redis8"
}
$RedisHome = (Get-ChildItem "$Tools\redis8" -Recurse -Filter 'redis-server.exe' | Select-Object -First 1).DirectoryName; Ok "Redis: $RedisHome"
# HL7 FHIR validator wrapper (standalone jar)
New-Item -ItemType Directory -Force -Path "$Tools\validator" | Out-Null
if (-not (Test-Path "$Tools\validator\validator-wrapper.jar")) { Download 'https://github.com/hapifhir/org.hl7.fhir.validator-wrapper/releases/download/1.0.85/validator-wrapper.jar' "$Tools\validator\validator-wrapper.jar" }
Ok "validator-wrapper.jar"
# Ruby 3.3 + MSYS2 (per-user install, only needed for Inferno). An existing RubyInstaller 3.3 on the
# machine is reused (the installer refuses a second silent install), otherwise it is installed under tools.
function Find-ExistingRuby {
  foreach ($hive in 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall', 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall') {
    Get-ChildItem $hive -ErrorAction SilentlyContinue | ForEach-Object {
      $i = Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
      if ($i.DisplayName -like 'Ruby 3.3*' -and $i.InstallLocation -and (Test-Path (Join-Path $i.InstallLocation 'bin\ruby.exe'))) { return $i.InstallLocation.TrimEnd('\') }
    }
  }
  return $null
}
$RubyHome = "$Tools\Ruby33-x64"
if (-not $SkipInferno) {
  if ($RubyHomeOverride) { $RubyHome = $RubyHomeOverride }
  elseif (-not (Test-Path "$RubyHome\bin\ruby.exe")) {
    $existing = Find-ExistingRuby
    if ($existing) { $RubyHome = $existing; Ok "reusing existing Ruby install: $RubyHome" }
    else {
      Download 'https://github.com/oneclick/rubyinstaller2/releases/download/RubyInstaller-3.3.13-1/rubyinstaller-devkit-3.3.13-1-x64.exe' "$Dl\rubyinstaller-devkit-3.3.13-1-x64.exe"
      Run "$Dl\rubyinstaller-devkit-3.3.13-1-x64.exe" "/VERYSILENT /CURRENTUSER /NORESTART /NOICONS /SP- /SUPPRESSMSGBOXES /DIR=`"$RubyHome`" /TASKS=`"noassocfiles,nomodpath`"" $Tools $null
    }
  }
  if (-not (Test-Path "$RubyHome\msys64\ucrt64\bin\gcc.exe")) {
    Write-Host "    installing MSYS2 toolchain for native gems (ridk install 1 3) - several minutes"
    Run "$env:ComSpec" "/c ridk.cmd install 1 3" "$RubyHome\bin" $null   # .cmd files must be run through cmd.exe (cwd avoids quoting issues)
  }
  Set-Content -Encoding ASCII "$Tools\ruby-home.txt" $RubyHome   # read by scripts\start-all.ps1
  Ok "Ruby: $RubyHome"
}

# env helper scripts
@"
# Dot-source this in PowerShell: . $Tools\env.ps1
`$env:JAVA_HOME = "$JavaHome"
`$env:MAVEN_HOME = "$MavenHome"
`$env:PGHOME = "$Tools\pgsql"
`$env:PATH = "`$env:JAVA_HOME\bin;`$env:MAVEN_HOME\bin;`$env:PGHOME\bin;`$env:PATH"
"@ | Set-Content -Encoding ASCII "$Tools\env.ps1"
function ToBashPath($p) { $p = $p -replace '\\', '/'; if ($p -match '^([A-Za-z]):(.*)$') { return '/' + $Matches[1].ToLower() + $Matches[2] } ; return $p }
@"
# Source this in Git Bash: source $(ToBashPath $Tools)/env.sh
export JAVA_HOME="$(ToBashPath $JavaHome)"
export MAVEN_HOME="$(ToBashPath $MavenHome)"
export PGHOME="$(ToBashPath $Tools)/pgsql"
export PATH="`$JAVA_HOME/bin:`$MAVEN_HOME/bin:`$PGHOME/bin:`$PATH"
"@ | Set-Content -Encoding ASCII "$Tools\env.sh"
$env:JAVA_HOME = $JavaHome
$env:PATH = "$JavaHome\bin;$MavenHome\bin;$Tools\pgsql\bin;$env:PATH"

# ------------------------------------------------------------------------------------------------
Step "2. PostgreSQL cluster + databases (user postgres / password postgres)"
$PgData = "$Tools\pgdata"
if (-not (Test-Path "$PgData\PG_VERSION")) {
  Set-Content -Encoding ASCII "$Tools\pgdata.pw" 'postgres'
  Run "$Tools\pgsql\bin\initdb.exe" "-D `"$PgData`" -U postgres --pwfile=`"$Tools\pgdata.pw`" -E UTF8 -A scram-sha-256" $Tools $null
  # pg_ctl start blocks when its console is redirected, so launch postgres.exe directly and wait with pg_isready
  Start-Process -FilePath "$Tools\pgsql\bin\postgres.exe" -ArgumentList "-D `"$PgData`" -p $PgInitPort" -WindowStyle Hidden -RedirectStandardOutput "$Tools\pg-setup-stdout.log" -RedirectStandardError "$Tools\pg-setup-stderr.log"
  $ready = $false
  for ($i = 0; $i -lt 30 -and -not $ready; $i++) { Start-Sleep -Seconds 2; & "$Tools\pgsql\bin\pg_isready.exe" -h localhost -p $PgInitPort -q; $ready = ($LASTEXITCODE -eq 0) }
  if (-not $ready) { throw "PostgreSQL did not start on port $PgInitPort (see $Tools\pg-setup-stderr.log)" }
  Run "$Tools\pgsql\bin\psql.exe" "-U postgres -h localhost -p $PgInitPort -c `"create database ecrnow;`" -c `"create database inferno;`"" $Tools @{ PGPASSWORD = 'postgres' }
  Run "$Tools\pgsql\bin\pg_ctl.exe" "-D `"$PgData`" stop -m fast" $Tools $null
  Start-Sleep -Seconds 2
}
Ok "cluster at $PgData (databases ecrnow, inferno)"

# ------------------------------------------------------------------------------------------------
Step "3. Source repositories (pinned to the commits used for the assessment)"
$repos = @(
  @{ dir = 'hapi-fhir-jpaserver-starter'; url = 'https://github.com/hapifhir/hapi-fhir-jpaserver-starter.git'; sha = '8db622874c2d1e3d26fe636978e00d77b75c0d2b' },
  @{ dir = 'eCRNow';                      url = 'https://github.com/drajer-health/eCRNow.git';                      sha = '3358f8bdf3b9c73c0d9ab04597e57e4cfbb3f8aa' },
  @{ dir = 'us-core-test-kit';            url = 'https://github.com/inferno-framework/us-core-test-kit.git';        sha = '87ddb5358b73a851fc64b765963dbccab41e50a2' },
  @{ dir = 'uds-plus-test-kit';           url = 'https://github.com/inferno-framework/uds-plus-test-kit.git';       sha = '34939a62878b0d5bffd77c127ba6845dd3cedfb9' }
)
foreach ($r in $repos) {
  if ($SkipInferno -and $r.dir -like '*test-kit') { continue }
  $d = Join-Path $Root $r.dir
  if (-not (Test-Path "$d\.git")) {
    git clone -q $r.url $d
    git -C $d -c advice.detachedHead=false checkout -q $r.sha
  }
  Ok "$($r.dir) @ $(git -C $d rev-parse --short HEAD)"
}

Step "3b. Local patches (JDK 21 Lombok fix, Windows fixes for the Inferno kits)"
$patches = @(
  @{ dir = 'eCRNow';            file = 'ecrnow-pom-lombok.patch' },
  @{ dir = 'us-core-test-kit';  file = 'us-core-test-kit-windows.patch' },
  @{ dir = 'uds-plus-test-kit'; file = 'uds-plus-test-kit-windows.patch' }
)
foreach ($p in $patches) {
  if ($SkipInferno -and $p.dir -like '*test-kit') { continue }
  $d = Join-Path $Root $p.dir; $pf = Join-Path $PSScriptRoot "patches\$($p.file)"
  # git writes "patch failed" to stderr when the patch is already applied; do not let that become a terminating error
  $prevEap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
  $null = & git -C $d apply --check $pf 2>&1
  $applicable = ($LASTEXITCODE -eq 0)
  if ($applicable) { $null = & git -C $d apply $pf 2>&1 }
  $ErrorActionPreference = $prevEap
  if ($applicable) { Ok "applied $($p.file)" } else { Ok "$($p.file) already applied (or not applicable)" }
}
# the US Core kit's .env carries an absolute path -> point it at this root
$envFile = Join-Path $Root 'us-core-test-kit\.env.development'
if (Test-Path $envFile) { (Get-Content $envFile -Raw) -replace 'C:/git/us-core-test-kit', "$RootFwd/us-core-test-kit" | Set-Content -Encoding ASCII $envFile }

# ------------------------------------------------------------------------------------------------
Step "4. Runtime folders and configuration"
$HapiRt = "$Root\hapi-runtime"; $EcrRt = "$Root\ecrnow-runtime"; $InfRt = "$Root\inferno-runtime"
New-Item -ItemType Directory -Force -Path "$HapiRt\h2", "$EcrRt\kars\ersd", "$EcrRt\bsa-output", "$EcrRt\custom-queries", "$EcrRt\logs", "$EcrRt\scheduled-task", "$EcrRt\schematrons", $InfRt | Out-Null
(Get-Content "$PSScriptRoot\templates\hapi-application-local.yaml" -Raw) -replace '\{\{ROOT\}\}', $RootFwd | Set-Content -Encoding ASCII "$HapiRt\application-local.yaml"
(Get-Content "$PSScriptRoot\templates\ecrnow-application-local.properties" -Raw) -replace '\{\{ROOT\}\}', $RootFwd | Set-Content -Encoding ASCII "$EcrRt\application-local.properties"
Copy-Item "$PSScriptRoot\kars\Bundle-bundle-ersd-specification-bundle.patched.json" "$EcrRt\kars\ersd\Bundle-bundle-ersd-specification-bundle.json" -Force
Copy-Item "$Root\eCRNow\documents\app-artifacts\CDAR2_IG_PHCASERPT_R2_STU1.1_SCHEMATRON.sch" "$EcrRt\schematrons\" -Force
Copy-Item "$Root\eCRNow\src\main\resources\custom-queries\*" "$EcrRt\custom-queries\" -Force -ErrorAction SilentlyContinue
Ok "hapi-runtime, ecrnow-runtime (patched eRSD KAR installed), inferno-runtime"

# ------------------------------------------------------------------------------------------------
Step "5. Build HAPI FHIR JPA starter (Maven, ~5-10 min first time)"
if (-not (Test-Path "$Root\hapi-fhir-jpaserver-starter\target\ROOT.war")) {
  Run "$MavenHome\bin\mvn.cmd" "-q -B clean package -DskipTests" "$Root\hapi-fhir-jpaserver-starter" @{ JAVA_HOME = $JavaHome }
}
Ok "hapi-fhir-jpaserver-starter\target\ROOT.war"

Step "6. Build eCRNow (Maven, ~5-10 min first time)"
if (-not (Test-Path "$Root\eCRNow\target\ecr-now.war")) {
  Run "$MavenHome\bin\mvn.cmd" "-q -B clean package -DskipTests -Dmaven.test.skip=true -Dspotless.check.skip=true" "$Root\eCRNow" @{ JAVA_HOME = $JavaHome }
}
Ok "eCRNow\target\ecr-now.war"

# ------------------------------------------------------------------------------------------------
if (-not $SkipInferno) {
  Step "7. Inferno test kits: gems + database migrations (~5 min)"
  $rubyEnv = @{ PATH = "$RubyHome\bin;$RubyHome\msys64\ucrt64\bin;$RubyHome\msys64\usr\bin;$env:PATH"; RI_DEVKIT = "$RubyHome\msys64"; MSYSTEM = 'UCRT64' }
  foreach ($kit in 'us-core-test-kit', 'uds-plus-test-kit') {
    $d = Join-Path $Root $kit
    if (-not (Test-Path "$d\data\inferno_development.db")) {
      # bundler is a Ruby script: run it with ruby.exe directly (no .bat / cmd.exe quoting problems)
      Run "$RubyHome\bin\ruby.exe" "`"$RubyHome\bin\bundle`" install --jobs 8" $d $rubyEnv
      Run "$RubyHome\bin\ruby.exe" "`"$RubyHome\bin\bundle`" exec inferno migrate" $d $rubyEnv
    }
    Ok "$kit ready"
  }
}

# ------------------------------------------------------------------------------------------------
if (-not $SkipDotnet) {
  Step "8. .NET integration layer (dotnet build)"
  Run 'dotnet' "build `"$Package\dotnet\FhirIntegration.sln`" -c Debug --nologo -v q" "$Package\dotnet" $null
  Ok "FhirIntegration.Api built"
}

Stop-Transcript | Out-Null
Write-Host @"

Setup complete. Next:
  powershell -ExecutionPolicy Bypass -File "$Package\scripts\start-all.ps1"      # starts everything (first start 3-5 min)
  curl -X POST -H "Content-Type: application/fhir+json" --data-binary @"$Package\sample-data\transaction-bundle.json" http://localhost:8080/fhir
  curl -X POST -H "Content-Type: application/fhir+json" --data-binary @"$Package\sample-data\transaction-bundle-2.json" http://localhost:8080/fhir
  python "$Package\scripts\fhir-demo.py"
  python "$Package\scripts\ecrnow-demo.py"
See README.md for the rest. Log: $LogFile
"@ -ForegroundColor Yellow
