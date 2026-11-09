<#
.SYNOPSIS
  Starts the complete local healthcare-interoperability stack (no Docker required).
  Run bootstrap\setup.ps1 once before using this script on a new machine.

  Services and ports
    PostgreSQL 16 (portable)      localhost:5432   databases: ecrnow, inferno
    Redis 8 (Windows build)       localhost:6379   Inferno / Sidekiq job queue
    HAPI FHIR R4 JPA server       http://localhost:8080/fhir   (UI: http://localhost:8080/)
    eCRNow (electronic case rpt)  http://localhost:8081        (Swagger: /swagger-ui/index.html)
    HL7 FHIR validator wrapper    http://localhost:8082        (used by Inferno + .NET layer)
    Inferno - US Core test kit    http://localhost:4567
    Inferno - UDS+ test kit       http://localhost:4568
    .NET FHIR integration layer   http://localhost:5080        (Swagger: /swagger)

  Usage:  powershell -ExecutionPolicy Bypass -File <package>\scripts\start-all.ps1 [-SkipInferno] [-SkipEcrNow] [-SkipDotnet] [-SkipValidator]
  Paths are derived from the script location: <Root>\interop-assessment\scripts\start-all.ps1 -> <Root>\tools, <Root>\hapi-runtime, ...
#>
param(
  [switch]$SkipInferno,
  [switch]$SkipEcrNow,
  [switch]$SkipDotnet,
  [switch]$SkipValidator
)

$ErrorActionPreference = 'Stop'
$Package = Split-Path -Parent $PSScriptRoot        # ...\interop-assessment
$Root    = Split-Path -Parent $Package             # ...\ (e.g. C:\git) - populated by bootstrap\setup.ps1
$Tools   = Join-Path $Root 'tools'
$Logs    = Join-Path $Package 'logs'
New-Item -ItemType Directory -Force -Path $Logs | Out-Null

# ---- portable toolchain --------------------------------------------------------------
$JavaHome = (Get-ChildItem $Tools -Directory -Filter 'jdk-21*' | Select-Object -First 1).FullName
if (-not $JavaHome) { throw "JDK 21 not found under $Tools - run bootstrap\setup.ps1 first" }
$env:JAVA_HOME = $JavaHome
$env:PATH      = "$JavaHome\bin;$Tools\apache-maven-3.9.9\bin;$Tools\pgsql\bin;$env:PATH"
$RubyHome      = Join-Path $Tools 'Ruby33-x64'
if (Test-Path (Join-Path $Tools 'ruby-home.txt')) { $RubyHome = (Get-Content (Join-Path $Tools 'ruby-home.txt') -Raw).Trim() }   # written by bootstrap\setup.ps1
$RedisHome     = (Get-ChildItem (Join-Path $Tools 'redis8') -Recurse -Filter 'redis-server.exe' | Select-Object -First 1).DirectoryName
$HapiRuntime   = Join-Path $Root 'hapi-runtime'
$EcrRuntime    = Join-Path $Root 'ecrnow-runtime'
$HapiRuntimeFwd = $HapiRuntime -replace '\\', '/'
$EcrRuntimeFwd  = $EcrRuntime -replace '\\', '/'

function Wait-Http($url, $seconds, $label) {
  $deadline = (Get-Date).AddSeconds($seconds)
  while ((Get-Date) -lt $deadline) {
    try { $r = Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 5; if ($r.StatusCode -eq 200) { Write-Host "  [OK] $label -> $url"; return $true } } catch { }
    Start-Sleep -Seconds 3
  }
  Write-Warning "  [TIMEOUT] $label did not answer at $url within $seconds s (check $Logs)"
  return $false
}
function Port-Open($port) { return [bool](Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) }

# ---- 1. PostgreSQL -------------------------------------------------------------------
Write-Host "1) PostgreSQL"
if (-not (Port-Open 5432)) {
  # postgres.exe is launched directly (pg_ctl start blocks when its console is redirected); stop-all.ps1 uses pg_ctl stop
  Start-Process -FilePath "$Tools\pgsql\bin\postgres.exe" -ArgumentList "-D `"$Tools\pgdata`" -p 5432" -WindowStyle Hidden -RedirectStandardOutput "$Logs\postgres.log" -RedirectStandardError "$Logs\postgres.err.log"
  for ($i = 0; $i -lt 30; $i++) { Start-Sleep -Seconds 2; & "$Tools\pgsql\bin\pg_isready.exe" -h localhost -p 5432 -q; if ($LASTEXITCODE -eq 0) { break } }
}
Write-Host "  [OK] postgres listening on 5432 (user postgres / password postgres)"

# ---- 2. Redis ------------------------------------------------------------------------
Write-Host "2) Redis 8"
if (-not (Port-Open 6379)) {
  Start-Process -FilePath "$RedisHome\redis-server.exe" -ArgumentList '--port 6379 --bind 127.0.0.1 --save "" --appendonly no' -WorkingDirectory $RedisHome -WindowStyle Hidden -RedirectStandardOutput "$Logs\redis.log" -RedirectStandardError "$Logs\redis.err.log"
  Start-Sleep -Seconds 2
}
Write-Host "  [OK] redis listening on 6379"

# ---- 3. HAPI FHIR --------------------------------------------------------------------
Write-Host "3) HAPI FHIR R4 JPA server (first start takes ~60-90 s)"
if (-not (Port-Open 8080)) {
  Start-Process -FilePath "$JavaHome\bin\java.exe" `
    -ArgumentList '-Xmx3g', '-jar', "`"$Root\hapi-fhir-jpaserver-starter\target\ROOT.war`"", "--spring.config.additional-location=file:$HapiRuntimeFwd/application-local.yaml" `
    -WorkingDirectory "$Root\hapi-fhir-jpaserver-starter" -WindowStyle Hidden `
    -RedirectStandardOutput "$Logs\hapi.log" -RedirectStandardError "$Logs\hapi.err.log"
}

# ---- 4. HL7 validator wrapper ----------------------------------------------------------
if (-not $SkipValidator) {
  Write-Host "4) HL7 FHIR validator wrapper (port 8082)"
  if (-not (Port-Open 8082)) {
    Start-Process -FilePath "$JavaHome\bin\java.exe" -ArgumentList '-Xmx8g', '-jar', 'validator-wrapper.jar' `
      -WorkingDirectory "$Tools\validator" -WindowStyle Hidden `
      -RedirectStandardOutput "$Logs\validator.log" -RedirectStandardError "$Logs\validator.err.log"
  }
}

Wait-Http 'http://localhost:8080/fhir/metadata' 300 'HAPI FHIR metadata' | Out-Null

# ---- 5. eCRNow -----------------------------------------------------------------------
if (-not $SkipEcrNow) {
  Write-Host "5) eCRNow (port 8081, first start ~60 s; loads eRSD KARs from $EcrRuntime\kars)"
  if (-not (Port-Open 8081)) {
    Start-Process -FilePath "$JavaHome\bin\java.exe" `
      -ArgumentList '-Xmx2g', '-Dsecurity.key=test123', '-Djdbc.username=postgres', '-Djdbc.password=postgres', '-jar', "`"$Root\eCRNow\target\ecr-now.war`"", "--spring.config.additional-location=file:$EcrRuntimeFwd/application-local.properties" `
      -WorkingDirectory $EcrRuntime -WindowStyle Hidden `
      -RedirectStandardOutput "$Logs\ecrnow.log" -RedirectStandardError "$Logs\ecrnow.err.log"
  }
}

# ---- 6. Inferno test kits (Ruby) -----------------------------------------------------
if (-not $SkipInferno) {
  Write-Host "6) Inferno test kits (US Core on 4567, UDS+ on 4568; first start ~2 min each)"
  $env:PATH = "$RubyHome\bin;$RubyHome\msys64\ucrt64\bin;$RubyHome\msys64\usr\bin;$env:PATH"
  $env:RI_DEVKIT = "$RubyHome\msys64"; $env:MSYSTEM = 'UCRT64'
  # validator URL, Redis db and ports are configured in each kit's .env.development (patched by setup.ps1)
  if (-not (Port-Open 4567)) {
    Start-Process -FilePath "$RubyHome\bin\bundle.bat" -ArgumentList 'exec', 'puma' -WorkingDirectory "$Root\us-core-test-kit" -WindowStyle Hidden -RedirectStandardOutput "$Logs\inferno-uscore-web.log" -RedirectStandardError "$Logs\inferno-uscore-web.err.log"
    Start-Process -FilePath "$RubyHome\bin\bundle.bat" -ArgumentList 'exec', 'sidekiq', '-r', './worker.rb' -WorkingDirectory "$Root\us-core-test-kit" -WindowStyle Hidden -RedirectStandardOutput "$Logs\inferno-uscore-worker.log" -RedirectStandardError "$Logs\inferno-uscore-worker.err.log"
  }
  if (-not (Port-Open 4568)) {
    Start-Process -FilePath "$RubyHome\bin\bundle.bat" -ArgumentList 'exec', 'puma', '-p', '4568' -WorkingDirectory "$Root\uds-plus-test-kit" -WindowStyle Hidden -RedirectStandardOutput "$Logs\inferno-udsplus-web.log" -RedirectStandardError "$Logs\inferno-udsplus-web.err.log"
    Start-Process -FilePath "$RubyHome\bin\bundle.bat" -ArgumentList 'exec', 'sidekiq', '-r', './worker.rb' -WorkingDirectory "$Root\uds-plus-test-kit" -WindowStyle Hidden -RedirectStandardOutput "$Logs\inferno-udsplus-worker.log" -RedirectStandardError "$Logs\inferno-udsplus-worker.err.log"
  }
}

# ---- 7. .NET integration layer -------------------------------------------------------
if (-not $SkipDotnet) {
  Write-Host "7) .NET FHIR integration layer (port 5080)"
  if (-not (Port-Open 5080)) {
    Start-Process -FilePath 'dotnet' -ArgumentList 'run', '--project', "`"$Package\dotnet\FhirIntegration.Api`"", '--no-launch-profile' `
      -WorkingDirectory "$Package\dotnet" -WindowStyle Hidden `
      -RedirectStandardOutput "$Logs\dotnet.log" -RedirectStandardError "$Logs\dotnet.err.log"
  }
}

# ---- readiness -----------------------------------------------------------------------
Write-Host "`nWaiting for services..."
if (-not $SkipEcrNow)    { Wait-Http 'http://localhost:8081/swagger-ui/index.html' 240 'eCRNow' | Out-Null }
if (-not $SkipValidator) { Wait-Http 'http://localhost:8082/' 120 'Validator wrapper' | Out-Null }
if (-not $SkipInferno)   { Wait-Http 'http://localhost:4567/' 300 'Inferno US Core' | Out-Null; Wait-Http 'http://localhost:4568/' 300 'Inferno UDS+' | Out-Null }
if (-not $SkipDotnet)    { Wait-Http 'http://localhost:5080/health' 180 '.NET integration layer' | Out-Null }

Write-Host "`nDone. Open http://localhost:8080/ (HAPI), http://localhost:8081/swagger-ui/index.html (eCRNow), http://localhost:4567/ (Inferno US Core), http://localhost:4568/ (Inferno UDS+), http://localhost:5080/swagger (.NET)."
