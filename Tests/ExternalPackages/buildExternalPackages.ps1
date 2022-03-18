
function BuildExternalPackage ([string]$packageFolder)
{
    pushd .
	Write-Host $packageFolder
    cd ".\$packageFolder"

    dotnet build --configuration Release
 
    Get-ChildItem -Path .\bin\Release\*.nupkg | Move-Item -Force -Destination ..\$packageFolder.1.0.0.nupkg

    popd
}

Get-ChildItem -Directory | ForEach-Object { BuildExternalPackage $_ }
