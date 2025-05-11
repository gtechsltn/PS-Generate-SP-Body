# PS-Generate-SP-Body

PowerShell script that generates the full T-SQL body of all stored procedures in a SQL Server database

* Microsoft.SqlServer.SMO
* Microsoft.SqlServer.Management.Sdk.Sfc
* Microsoft.SqlServer.ConnectionInfo
* Microsoft.SqlServer.SmoExtended

## PowerShell Script — Template
```
Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy RemoteSigned -Force

Clear-Host

$CurrentDir = (Get-Location).Path
Write-Host "Get-Location into CurrentDir variable: DONE" -ForegroundColor DarkGreen -BackgroundColor Black

Try {    
    # Code that may throw an exception
    # TODO: 
}
Catch [System.Exception] {
    # Code to handle the error
    # Write-Host $_.Exception.Message -ForegroundColor Red -BackgroundColor Yellow
}
Finally {
    # Code that runs regardless of an error occurring or not

    Set-Location -Path $CurrentDir -PassThru
    Write-Host "Set-Location -Path '$CurrentDir' : DONE" -ForegroundColor DarkGreen -BackgroundColor Black
    Write-Host "------------------------------------------------------------" -ForegroundColor DarkGreen -BackgroundColor Black
}
```

## PowerShell Script — Generate All Stored Procedure Bodies
```
# === CONFIGURATION ===
$serverName = "(local)"             # SQL Server name or instance (e.g., "localhost\SQLEXPRESS")
$databaseName = "YourDatabaseName"  # Database name
$outputFolder = "C:\StoredProceduresExport"  # Output folder path for .sql files

# === PREPARE OUTPUT FOLDER ===
if (!(Test-Path -Path $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

# === LOAD SMO ASSEMBLIES ===
Add-Type -AssemblyName "Microsoft.SqlServer.SMO"
Add-Type -AssemblyName "Microsoft.SqlServer.Management.Sdk.Sfc"
Add-Type -AssemblyName "Microsoft.SqlServer.ConnectionInfo"
Add-Type -AssemblyName "Microsoft.SqlServer.SmoExtended"

# === CONNECT TO SQL SERVER ===
$server = New-Object Microsoft.SqlServer.Management.Smo.Server $serverName
$db = $server.Databases[$databaseName]

if ($db -eq $null) {
    Write-Error "Database '$databaseName' not found on server '$serverName'"
    exit
}

Write-Host "Connected to server '$serverName', database '$databaseName'"

# === SCRIPT EACH STORED PROCEDURE ===
foreach ($sp in $db.StoredProcedures) {
    if (!$sp.IsSystemObject) {
        $schemaName = $sp.Schema
        $procName = $sp.Name
        $fileName = "${schemaName}.${procName}.sql"
        $filePath = Join-Path $outputFolder $fileName

        # Configure scripter
        $scripter = New-Object Microsoft.SqlServer.Management.Smo.Scripter ($server)
        $scripter.Options.ScriptDrops = $false
        $scripter.Options.IncludeHeaders = $true
        $scripter.Options.SchemaQualify = $true
        $scripter.Options.NoFileGroup = $true
        $scripter.Options.AnsiPadding = $true
        $scripter.Options.Encoding = [System.Text.Encoding]::UTF8

        # Script procedure
        $script = $scripter.Script($sp)
        $scriptText = $script -join "`r`n"

        # Write to file
        $scriptText | Out-File -FilePath $filePath -Encoding UTF8

        Write-Host "Scripted: $schemaName.$procName -> $fileName"
    }
}

Write-Host "`n✅ All stored procedures scripted to: $outputFolder"
```
