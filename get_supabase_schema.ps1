# PowerShell script to export Supabase schema
# Usage: .\get_supabase_schema.ps1 -ProjectUrl "https://xxxxx.supabase.co" -ServiceRoleKey "YOUR_SERVICE_ROLE_KEY"

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$ServiceRoleKey
)

# Get all tables
$TablesUri = "$ProjectUrl/rest/v1/information_schema.tables?schema=eq.public&select=table_name"
$Headers = @{
    "apikey" = $ServiceRoleKey
    "Authorization" = "Bearer $ServiceRoleKey"
}

Write-Host "Fetching tables..." -ForegroundColor Cyan

$Tables = Invoke-RestMethod -Uri $TablesUri -Headers $Headers | Select-Object -ExpandProperty table_name

foreach ($Table in $Tables) {
    Write-Host "Table: $Table" -ForegroundColor Green
    
    # Get columns for each table
    $ColumnsUri = "$ProjectUrl/rest/v1/information_schema.columns?table_name=eq.$Table&schema=eq.public"
    $Columns = Invoke-RestMethod -Uri $ColumnsUri -Headers $Headers
    
    foreach ($Col in $Columns) {
        Write-Host "  - $($Col.column_name) ($($Col.data_type))" -ForegroundColor Yellow
    }
    Write-Host ""
}

Write-Host "Complete schema exported" -ForegroundColor Cyan
