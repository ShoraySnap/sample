# Define the path to the MSBuild executable for Visual Studio
$msBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# Get the current script's directory
$currentScriptPath = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent

# Define the path to the solution file relative to the script's location (one level up)
$solutionPath = Join-Path -Path $currentScriptPath -ChildPath "..\SnaptrudeManagerAddin.sln"

# Names of the projects within the solution
$projectNames = @("SnaptrudeManagerAddin", "SnaptrudeForgeExport")  # Replace with actual project names

# Build configurations from 2019 to 2024
$configurations = @("2019", "2020", "2021", "2022", "2023", "2024")

# Spinner setup
$spinner = @('-', '/', '|', '\')
$spinnerIndex = 0
$done = $false

# Function to display spinner at the beginning of the line
$spinnerJob = {
    param($spinner, [ref]$spinnerIndex, [ref]$done)
    while (-not $done.Value) {
        $currentSpinner = $spinner[$spinnerIndex.Value++ % $spinner.Length]
        # Reset cursor to the beginning of the line before writing the spinner
        Write-Host "`r$currentSpinner" -NoNewline
        Start-Sleep -Milliseconds 100
    }
    # Clear the spinner after completion
    Write-Host "`r " -NoNewline  
}

# Start spinner job
$job = Start-Job -ScriptBlock $spinnerJob -ArgumentList $spinner, ([ref]$spinnerIndex), ([ref]$done)

# Main compilation logic
try {
    foreach ($projectName in $projectNames) {
        foreach ($config in $configurations) {
            # Display compilation message without newline to stay on the same line as the spinner
            Write-Host "`r$spinnerChar Compiling $projectName with configuration $config..." -NoNewline
            # Execute MSBuild and capture output
            $output = & $msBuildPath $solutionPath /t:$projectName /p:Configuration=$config /p:Platform="Any CPU" /clp:NoSummary *>&1

            # Extract errors and warnings from the output
            $errors = ($output | Select-String "error MSB").Count
            $warnings = ($output | Select-String "warning MSB").Count

            # Check for errors and handle immediately
            if ($errors -gt 0) {
                Write-Host "Error in $projectName with configuration $config:" -ForegroundColor Red
                $output | Where-Object { $_ -match "error MSB" } | ForEach-Object { Write-Host $_ -ForegroundColor Red }
                $done.Value = $true
                return
            }

            # Clear line and prepare for next message or success message
            Write-Host "`r$message - Done" -ForegroundColor Green
        }
    }
}
finally {
    # Stop the spinner
    $done.Value = $true
    Wait-Job $job
    Remove-Job $job
    Write-Host "`rCompilation of all projects completed successfully without errors." -ForegroundColor Green
}

# Clear any remaining spinner or output clutter
Write-Host "`r " -NoNewline