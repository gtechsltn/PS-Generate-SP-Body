
Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy RemoteSigned -Force

Clear-Host

$CurrentDir = (Get-Location).Path
Write-Host "Get-Location into CurrentDir variable: DONE" -ForegroundColor DarkGreen -BackgroundColor Black

Try {
    # === CONFIGURATION ===
    $serverName = "MANH"             # SQL Server name or instance
    $databaseName = "mssql"  # Your target database
    $outputFolder = "D:\StoredProceduresExport"  # Where to save the .sql files

    # === PREPARE OUTPUT FOLDER ===
    if (!(Test-Path -Path $outputFolder)) {
        New-Item -ItemType Directory -Path $outputFolder | Out-Null
    }

    # === LOAD SMO LIBRARIES (Reliable way) ===
    $SmoAssemblyPath = "$([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory())"

    # Load the SMO libraries from the GAC or installed location
    Add-Type -AssemblyName "Microsoft.SqlServer.Smo"
    Add-Type -AssemblyName "Microsoft.SqlServer.Management.Sdk.Sfc"
    Add-Type -AssemblyName "Microsoft.SqlServer.ConnectionInfo"
    Add-Type -AssemblyName "Microsoft.SqlServer.SmoExtended"

    # === CONNECT TO SQL SERVER ===
    $server = New-Object Microsoft.SqlServer.Management.Smo.Server $serverName

    # Optional: Uncomment and set SQL authentication if needed
    # $server.ConnectionContext.LoginSecure = $false
    # $server.ConnectionContext.set_Login('your_username')
    # $server.ConnectionContext.set_SecurePassword(('your_password' | ConvertTo-SecureString -AsPlainText -Force))

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


}
Catch [System.Exception] {
    Write-Host $_.Exception.Message -ForegroundColor Red -BackgroundColor Yellow
}
Finally {
    Set-Location -Path $CurrentDir -PassThru
    Write-Host "Set-Location -Path '$CurrentDir' : DONE" -ForegroundColor DarkGreen -BackgroundColor Black
    Write-Host "------------------------------------------------------------" -ForegroundColor DarkGreen -BackgroundColor Black
}