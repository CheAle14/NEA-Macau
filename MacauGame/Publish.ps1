function ExitWithCode { param($exitcode) $host.SetShouldExit($exitcode); exit }
Write-Host "[01] You must build the project in VisualStudio first."
Write-Host "     Press any key once you have done so."
Read-Host
Write-Host "[02] Assuming you have built, packaging..."
Remove-Item -Recurse -Force ./temp
nuget.exe pack MacauGame/MacauGame.nuspec -OutputDirectory ./temp
if($?) {
    Write-Host "Successfully packaged."
} else {
    Write-Host "[03] ERROR - Nuget packaging failed."
    ExitWithCode 1
}
$files = Get-ChildItem ./temp
$firstFile = $files[0]
Write-Host $firstFile
cd ./temp
Write-Host "Invoking squirrels..."
Squirrel --releasify $firstFile -r ../Releases
if($?) {
    Write-Host "Successfully releasified."
} else {
    Write-Host "== ERROR: Failed to releasify"
    ExitWithCode 2
}
cd ..

function Get-Token {
    if (Test-Path -Path "./github.token") {
        $content = Get-Content "./github.token"
        $content = $content.Trim()
        Write-Output -InputObject $content
    } else {
        Write-Host "To upload to github, you must provide a personal access token; please enter one now."
        $token = Read-Host "Personal Access Token"
        $token = $token.Trim()
        Out-File -FilePath "./github.token" -InputObject $token
        Write-Output $token
    }
}

$token = Get-Token
cd Releases
git add --all
git commit -m "Publish $($firstFile)"
git remote set-url origin "https://CheAle14:$($token)@github.com/CheAle14/nea-builds.git"
git push


Write-Host "Press any key to exit."
Read-Host
