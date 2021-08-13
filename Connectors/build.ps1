param (
	[string]$configuration = "Debug"
)

$outputFolder = "$PSScriptRoot\bin\$configuration"

Remove-Item $outputFolder -Recurse -Force -ErrorAction SilentlyContinue

mkdir $outputFolder 

# build V1 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V1

dotnet publish -c $configuration

mkdir $outputFolder\V1\
Copy-Item bin\$configuration\net452\publish\* $outputFolder\V1\ -Exclude @('TechTalk.*','System.*', 'Gherkin.*','*.exe.config')

# build V1 x86

Remove-Item bin\$configuration\net452\win-x86\publish -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish -r win-x86 -c $configuration /p:PlatformTarget=x86

Rename-Item bin\$configuration\net452\win-x86\publish\specflow-vs.exe specflow-vs-x86.exe -Force
Rename-Item bin\$configuration\net452\win-x86\publish\specflow-vs.pdb specflow-vs-x86.pdb -Force

Copy-Item bin\$configuration\net452\win-x86\publish\specflow-vs-x86.* $outputFolder\V1\

cd ..

# build V2 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V2

dotnet publish -f netcoreapp2.1 -c $configuration

Copy-Item bin\$configuration\netcoreapp2.1\publish\ $outputFolder\V2-netcoreapp2.1\ -Recurse

dotnet publish -f netcoreapp3.1 -c $configuration

Copy-Item bin\$configuration\netcoreapp3.1\publish\ $outputFolder\V2-netcoreapp3.1\ -Recurse

dotnet publish -f net5.0 -c $configuration

Copy-Item bin\$configuration\net5.0\publish\ $outputFolder\V2-net5.0\ -Recurse

cd ..

# build V3 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V3

dotnet publish -f netcoreapp2.1 -c $configuration

Copy-Item bin\$configuration\netcoreapp2.1\publish\ $outputFolder\V3-netcoreapp2.1\ -Recurse

dotnet publish -f netcoreapp3.1 -c $configuration

Copy-Item bin\$configuration\netcoreapp3.1\publish\ $outputFolder\V3-netcoreapp3.1\ -Recurse

dotnet publish -f net5.0 -c $configuration

Copy-Item bin\$configuration\net5.0\publish\ $outputFolder\V3-net5.0\ -Recurse

cd ..
