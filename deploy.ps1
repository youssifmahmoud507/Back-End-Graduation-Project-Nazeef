# deploy.ps1
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   NdeefApp Automated Deployment Script 🚀   " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Check if logged in to Azure CLI
try {
    $azAccount = az account show --query name -o tsv 2>$null
    if (-not $azAccount) {
        Write-Host "[x] You are not logged in to Azure CLI. Please run 'az login' first." -ForegroundColor Red
        exit 1
    }
    Write-Host "[+] Logged in to Azure subscription: $azAccount" -ForegroundColor Green
} catch {
    Write-Host "[x] Azure CLI is not installed or not in PATH. Please install Azure CLI." -ForegroundColor Red
    exit 1
}

# 1. Stage and commit changes
Write-Host "`n1. Staging and committing changes..." -ForegroundColor Cyan
git add .
$commitMsg = Read-Host "Enter commit message (default: 'Auto deployment update')"
if ([string]::IsNullOrEmpty($commitMsg)) {
    $commitMsg = "Auto deployment update"
}
git commit -m "$commitMsg"

# 2. Push to GitHub
Write-Host "`n2. Pushing changes to GitHub master branch..." -ForegroundColor Cyan
git push origin master

# 3. Wait for GitHub Actions to build and push image
Write-Host "`n3. Waiting for GitHub Actions to build and push the Docker image to GHCR..." -ForegroundColor Cyan
Write-Host "This usually takes about 2-3 minutes. Checking status..." -ForegroundColor Yellow

$repo = "baselAhmed2/NdeefAppGradProjectBackEnd"
$apiUrl = "https://api.github.com/repos/$repo/actions/runs?branch=master&event=push"
$githubToken = $env:GITHUB_TOKEN

if ([string]::IsNullOrWhiteSpace($githubToken)) {
    Write-Host "[x] GITHUB_TOKEN environment variable is not set. Please set it before running this script." -ForegroundColor Red
    exit 1
}

$headers = @{
    "User-Agent" = "PowerShell"
    "Authorization" = "token $githubToken"
}

# Wait a few seconds for the run to be registered on GitHub
Start-Sleep -Seconds 10

$timeout = [DateTime]::Now.AddMinutes(7)
$completed = $false

while ([DateTime]::Now -lt $timeout) {
    try {
        $response = Invoke-RestMethod -Uri $apiUrl -Method Get -Header $headers -UseBasicParsing
        $latestRun = $response.workflow_runs[0]
        
        $status = $latestRun.status
        $conclusion = $latestRun.conclusion
        
        if ($status -eq "completed") {
            if ($conclusion -eq "success") {
                Write-Host "`n[+] GitHub Actions build succeeded! Image is ready on GHCR." -ForegroundColor Green
                $completed = $true
                break
            } else {
                Write-Host "`n[x] GitHub Actions build failed with conclusion: $conclusion" -ForegroundColor Red
                Write-Host "Check details here: $($latestRun.html_url)" -ForegroundColor Yellow
                exit 1
            }
        } else {
            Write-Host "Build status: $status... Waiting 15 seconds." -ForegroundColor Gray
            Start-Sleep -Seconds 15
        }
    } catch {
        Write-Host "Polling status... Waiting 15 seconds." -ForegroundColor Gray
        Start-Sleep -Seconds 15
    }
}

if (-not $completed) {
    Write-Host "`n[x] Timeout waiting for GitHub Actions build." -ForegroundColor Red
    exit 1
}

# 4. Deploy the new image to Azure Container App
Write-Host "`n4. Updating Azure Container App with the new image..." -ForegroundColor Cyan
az containerapp update --name ndeefapp-api --resource-group tiketapp-rg --image ghcr.io/baselahmed2/ndeefappgradprojectbackend:latest --min-replicas 1

Write-Host "`n=============================================" -ForegroundColor Green
Write-Host "   Deployment Completed Successfully!      " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host "Your app is live at: https://ndeefapp-api.icydune-2fcf3dd1.germanywestcentral.azurecontainerapps.io" -ForegroundColor Green
