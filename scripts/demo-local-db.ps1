param(
    [string]$EnvFile = '.env'
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$composeFile = Join-Path $projectRoot 'docker-compose.yml'
$envFile = if ([IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $projectRoot $EnvFile }
$schemaFile = Join-Path $projectRoot 'database\database-schema.sql'
$seedFile = Join-Path $projectRoot 'database\seed-data.sql'
$queryFile = Join-Path $projectRoot 'database\demo-movies.sql'

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$Step
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Step thất bại với exit code $LASTEXITCODE."
    }
}

function Read-DotEnv {
    param([string]$Path)

    foreach ($line in Get-Content -LiteralPath $Path -Encoding UTF8) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)\s*$') {
            $name = $Matches[1]
            $value = $Matches[2].Trim()
            if ($value.Length -ge 2 -and (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'")))) {
                $value = $value.Substring(1, $value.Length - 2)
            }
            [Environment]::SetEnvironmentVariable($name, $value, 'Process')
        }
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Không tìm thấy Docker CLI. Hãy cài và khởi động Docker Desktop trước khi chạy demo.'
}

Invoke-Checked 'docker' @('--version') 'Kiểm tra Docker'
Invoke-Checked 'docker' @('compose', 'version') 'Kiểm tra Docker Compose'

foreach ($requiredFile in @($composeFile, $envFile, $schemaFile, $seedFile, $queryFile)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Thiếu file bắt buộc: $requiredFile"
    }
}

Read-DotEnv $envFile
foreach ($requiredVariable in @('POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD')) {
    $value = [Environment]::GetEnvironmentVariable($requiredVariable, 'Process')
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Thiếu biến $requiredVariable trong .env. Password không được in ra terminal."
    }
}

$dbName = $env:POSTGRES_DB
$dbUser = $env:POSTGRES_USER
$hostPort = if ([string]::IsNullOrWhiteSpace($env:POSTGRES_HOST_PORT)) { 5433 } else { [int]$env:POSTGRES_HOST_PORT }
$composeArgs = @('--project-directory', $projectRoot, '-f', $composeFile)

$runningContainer = & docker compose @composeArgs ps -q postgres
if ($null -eq $runningContainer) {
    $runningContainer = ''
} else {
    $runningContainer = ($runningContainer -join "`n").Trim()
}
if ([string]::IsNullOrWhiteSpace($runningContainer)) {
    $portInUse = Get-NetTCPConnection -LocalPort $hostPort -State Listen -ErrorAction SilentlyContinue
    if ($null -ne $portInUse) {
        throw "Port local $hostPort đang được sử dụng. Đổi POSTGRES_HOST_PORT trong .env hoặc dừng tiến trình đang dùng port; script không tự sửa/reset."
    }
}

Write-Host "Khởi động PostgreSQL local trên 127.0.0.1:$hostPort ..."
Invoke-Checked 'docker' (@('compose') + $composeArgs + @('up', '-d', 'postgres')) 'Khởi động PostgreSQL'

$deadline = (Get-Date).AddSeconds(120)
$ready = $false
while ((Get-Date) -lt $deadline) {
    & docker compose @composeArgs exec -T postgres pg_isready -U $dbUser -d $dbName *> $null
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}
if (-not $ready) {
    Write-Host 'Database chưa sẵn sàng. Log container gần nhất:'
    & docker compose @composeArgs logs --tail 100 postgres
    throw 'Timeout chờ PostgreSQL readiness; không tự reset volume.'
}

$verificationSql = @'
SELECT CASE WHEN to_regnamespace('cinema') IS NULL THEN 1 ELSE 0 END AS missing_schema;
SELECT CASE WHEN to_regclass('cinema.movie') IS NULL THEN 1 ELSE 0 END AS missing_movie_table;
'@
$verificationOutput = $verificationSql | & docker compose @composeArgs exec -T postgres psql -X -v ON_ERROR_STOP=1 -U $dbUser -d $dbName -At
if ($LASTEXITCODE -ne 0) {
    throw 'Xác nhận schema/bảng thất bại.'
}
if ($verificationOutput -match '(^|\r?\n)1(\r?\n|$)') {
    throw 'Schema cinema hoặc bảng cinema.movie chưa tồn tại; không tự sửa database.'
}

Write-Host 'Schema và bảng cinema.movie đã được xác nhận.'
Get-Content -LiteralPath $queryFile -Raw -Encoding UTF8 |
    & docker compose @composeArgs exec -T postgres psql -X -v ON_ERROR_STOP=1 -U $dbUser -d $dbName
if ($LASTEXITCODE -ne 0) {
    throw 'Truy vấn demo thất bại.'
}

Write-Host 'Demo hoàn tất. Dữ liệu nằm trong named volume cinema_demo_postgres_data; không có thao tác xóa/reset.'
