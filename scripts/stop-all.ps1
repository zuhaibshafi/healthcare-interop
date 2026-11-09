<#
.SYNOPSIS  Stops every service started by start-all.ps1 (by listening port), then PostgreSQL.
#>
$Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$ports = @(5080, 4568, 4567, 8082, 8081, 8080, 6379)
foreach ($p in $ports) {
  $conns = Get-NetTCPConnection -State Listen -LocalPort $p -ErrorAction SilentlyContinue
  foreach ($c in $conns) {
    $proc = Get-Process -Id $c.OwningProcess -ErrorAction SilentlyContinue
    if ($proc) { Write-Host "Stopping port $p -> $($proc.ProcessName) (PID $($proc.Id))"; Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
  }
}
# Inferno Sidekiq workers do not listen on a port: stop ruby processes running sidekiq
Get-CimInstance Win32_Process -Filter "Name = 'ruby.exe'" | Where-Object { $_.CommandLine -match 'sidekiq' } | ForEach-Object {
  Write-Host "Stopping sidekiq worker (PID $($_.ProcessId))"; Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

& "$Root\tools\pgsql\bin\pg_ctl.exe" -D "$Root\tools\pgdata" stop -m fast 2>$null
Write-Host "All services stopped."
