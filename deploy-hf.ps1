# deploy-hf.ps1
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "   NdeefApp Hugging Face Deployment Script 🚀" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# 1. Ask for Hugging Face Username
$defaultUsername = "baselAhmed2"
$username = Read-Host "Enter your Hugging Face Username (default: '$defaultUsername')"
if ([string]::IsNullOrEmpty($username)) {
    $username = $defaultUsername
}

# 2. Ask for Space Name
$defaultSpace = "ndeef-backend"
$spaceName = Read-Host "Enter your Space Name (default: '$defaultSpace')"
if ([string]::IsNullOrEmpty($spaceName)) {
    $spaceName = $defaultSpace
}

# 3. Ask for Hugging Face Write Token
Write-Host "`nYou need a Hugging Face Access Token with 'Write' permissions." -ForegroundColor Yellow
Write-Host "Get it here: https://huggingface.co/settings/tokens" -ForegroundColor Yellow
$hfToken = Read-Host "Enter your Hugging Face Write Token"
if ([string]::IsNullOrEmpty($hfToken)) {
    Write-Host "[x] Error: Hugging Face Token is required." -ForegroundColor Red
    exit 1
}

# 4. Stage and commit changes
Write-Host "`n1. Staging and committing changes..." -ForegroundColor Cyan
git add .
$commitMsg = Read-Host "Enter commit message (default: 'Deploy to Hugging Face')"
if ([string]::IsNullOrEmpty($commitMsg)) {
    $commitMsg = "Deploy to Hugging Face"
}
# Commit changes if there are any staged modifications
$status = git status --porcelain
if ($status) {
    git commit -m "$commitMsg"
} else {
    Write-Host "No changes to commit." -ForegroundColor Gray
}

# 5. Configure Hugging Face Remote
Write-Host "`n2. Configuring Hugging Face Remote..." -ForegroundColor Cyan
$remoteUrl = "https://$($username):$($hfToken)@huggingface.co/spaces/$username/$spaceName"

# Check if hf remote already exists
$existingRemote = git remote | Where-Object { $_ -eq "hf" }
if ($existingRemote) {
    git remote set-url hf $remoteUrl
} else {
    git remote add hf $remoteUrl
}

# 6. Push to Hugging Face Space
Write-Host "`n3. Pushing changes to Hugging Face..." -ForegroundColor Cyan
Write-Host "This will trigger the Docker build on Hugging Face Spaces." -ForegroundColor Yellow
git push -f hf master:main

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=============================================" -ForegroundColor Green
    Write-Host "   Pushed successfully!                      " -ForegroundColor Green
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "Monitor your deployment here: https://huggingface.co/spaces/$username/$spaceName" -ForegroundColor Green
    Write-Host "Your Webhook URL will be: https://$username-$spaceName.hf.space/api/Didit/webhook" -ForegroundColor Green
} else {
    Write-Host "`n[x] Push failed. Please check your token and credentials." -ForegroundColor Red
}
