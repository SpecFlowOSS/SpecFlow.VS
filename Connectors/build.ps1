param (
	[string]$configuration = "Debug"
)

$outputFolder = "$PSScriptRoot\bin\$configuration"

Remove-Item $outputFolder -Recurse -Force -ErrorAction SilentlyContinue

mkdir $outputFolder 

# build V1 any cpu

cd ..\Deveroom.VisualStudio.SpecFlowConnector.V1

dotnet publish -c $configuration

mkdir $outputFolder\V1\
Copy-Item bin\$configuration\net452\publish\* $outputFolder\V1\ -Exclude @('TechTalk.*','System.*', 'Gherkin.*','*.exe.config')

# build V1 x86

Remove-Item bin\$configuration\net452\win-x86\publish -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish -r win-x86 -c $configuration /p:PlatformTarget=x86

Rename-Item bin\$configuration\net452\win-x86\publish\deveroom-specflow-v1.exe deveroom-specflow-v1.x86.exe -Force
Rename-Item bin\$configuration\net452\win-x86\publish\deveroom-specflow-v1.pdb deveroom-specflow-v1.x86.pdb -Force

Copy-Item bin\$configuration\net452\win-x86\publish\deveroom-specflow-v1.x86.* $outputFolder\V1\

cd ..\Connectors

# build V2 any cpu

cd ..\Deveroom.VisualStudio.SpecFlowConnector.V2

dotnet publish -f netcoreapp2.1 -c $configuration

Copy-Item bin\$configuration\netcoreapp2.1\publish\ $outputFolder\V2\ -Recurse

dotnet publish -f netcoreapp3.1 -c $configuration

Copy-Item bin\$configuration\netcoreapp3.1\publish\ $outputFolder\V3\ -Recurse

dotnet publish -f net5.0 -c $configuration

Copy-Item bin\$configuration\net5.0\publish\ $outputFolder\V5\ -Recurse

cd ..\Connectors