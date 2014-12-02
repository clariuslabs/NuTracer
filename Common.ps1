# Builds all .slns recursively in Release mode, then 
# pushes all resulting .nupkg in bin\Release folders
# This function assumes it's called from the netfx root 
# folder, with the relative path being the path to 
# search for nugets.
function Push-Packages
{
	param([string] $relativePath)

	Build-Packages $relativePath

	Write-Progress -Activity "Pushing" -Status "Publishing built packages recursively"

	Get-ChildItem -Path $relativePath -Recurse -Filter *.nupkg | Where-Object { $_.DirectoryName.EndsWith("bin\Release") }  | %{ .\nuget.exe Push $_.FullName }

	Write-Progress -Activity "Pushing" -Status "Done!" -Completed
}

# Builds all .slns recursively in Release mode, then 
# copies all resulting .nupkg in bin\Release folders
# to the Drop folder.
# This function assumes it's called from the netfx root 
# folder, with the relative path being the path to 
# search for nugets.
function Drop-Packages
{
	param([string] $relativePath)

	Build-Packages $relativePath

	mkdir Drop -ea silentlycontinue
	$dropDir = gci -Filter Drop
	Write-Host "Drop location is " $dropDir.FullName

	Get-ChildItem -Path $current -Recurse -Filter *.nupkg | `
	Where-Object { $_.DirectoryName.EndsWith("bin\Release") }  | %{ `
		$target = [System.IO.Path]::Combine($dropDir.FullName, $_.Name); `
		[System.Threading.Thread]::Sleep(1000); `
		$_.CopyTo($target, $true); }

	Write-Progress -Activity "Deploying" -Status "Done!" -Completed
}

# Builds all .slns recursively in Release mode
# This function assumes it's called from the netfx root 
# folder, with the relative path being the path to 
# search for nugets.
function Build-Packages
{
	param([string] $relativePath)

	$msbuilds = @(get-command msbuild -ea SilentlyContinue)
	if ($msbuilds.Count -eq 0) {
		throw "MSBuild could not be found in the path. Please ensure MSBuild is in the path."
	}

	if (!(test-path ".\.nuget\NuGet.exe")) {
		if (!(test-path ".\.nuget")) {
			mkdir .nuget | out-null
		}
		write-host Downloading NuGet.exe...
		invoke-webrequest "https://nuget.org/nuget.exe" -outfile ".\.nuget\NuGet.exe"
	}

	if (test-path ".\packages.config") {
		write-host Installing root-level NuGet packages...
		.\.nuget\NuGet.exe Install -OutputDirectory packages -ExcludeVersion
	}

	$msbuild = $msbuilds[0].Definition
	
	Write-Progress -Activity "Building" -Status "Building release recursively"

	# Build all extensions
	foreach ($build in (Get-ChildItem -Path $relativePath -Recurse -Filter *.sln))
	{
		pushd $build.DirectoryName

			$progress++
			Write-Progress -Activity "Building" -Status ("Cleaning " + $build.Name) -PercentComplete $progress
			&$msbuild /target:Clean /verbosity:quiet /p:Configuration=Debug | out-null
			&$msbuild /target:Clean /verbosity:quiet /p:Configuration=Release | out-null

			Write-Progress -Activity "Building" -Status ("Building " + $build.Name) -PercentComplete $progress
			&$msbuild /verbosity:minimal /p:Configuration=Release /nologo
		
		popd
	}

	Write-Progress -Activity "Building" -Status "Done!" -Completed
}