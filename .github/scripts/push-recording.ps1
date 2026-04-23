param(
    [string]$RecordingsDir = "recordings"
)

git -C $PSScriptRoot pull origin master

$files = Get-ChildItem -Path $RecordingsDir -Filter *.webm -File | Sort-Object LastWriteTime -Descending
if ($files.Count -eq 0) {
    Write-Host "No recordings found in $RecordingsDir"
    exit 0
}

# take latest
$latest = $files[0].FullName
git -C $PSScriptRoot add $latest
git -C $PSScriptRoot commit -m "Add test recording $($files[0].Name)"
git -C $PSScriptRoot push origin master
Write-Host "Pushed $latest to origin"