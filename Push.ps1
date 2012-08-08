# ========================================================================
#	Pushes to nuget.org all packages from this folder down, built on 
#	Release mode.
#
#	This script looks up the directory tree searching for the NETFx root
#	which is signaled by the netfx.txt file.
#	Once the root is located, the Common.ps1 from there is imported 
#	in the context and the Push-Packages function in it is invoked.
# ========================================================================

$current = $pwd.Path
$paths = new-object System.Collections.Stack
$dir = new-object System.IO.DirectoryInfo $pwd.Path
$isRoot = gci nuget.exe -ea silentlycontinue

# Search for the root
while ($isRoot -eq $null)
{
	$paths.Push($dir.Name)
	$dir = $dir.Parent
	
	pushd ..\
	$isRoot = gci nuget.exe -ea silentlycontinue
}

. ./Common.ps1

# Push first drops, always. This keeps the offline 
# cache Drop always current whenever we push.
Drop-Packages $current

# Now push for real
Push-Packages $current

# Pop folders just in case this is invoked from a powershell prompt
$current = $pwd.Path
popd
while ($current -ne $pwd.Path)
{
	$current = $pwd.Path
	popd
}

write-host Done!