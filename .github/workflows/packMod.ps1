param ([string]$ModDir, [string]$Src)

$common = "Common"
$about = "About"
$loadFolder = "LoadFolders.xml"
$defs = "Defs"
$languages = "Languages"
$patches = "Patches"
$texture = "Textures"
$assemblies = "Assemblies"
$RwUtil = "NotooShabby.RimWorldUtility.dll"

# Copy About folder
Copy-Item -Path ([System.IO.Path]::Combine($Src, $about)) -Destination ([System.IO.Path]::Combine($ModDir, $about)) -Recurse -PassThru

# Copy LoadFolders.xml
Copy-Item ([System.IO.Path]::Combine($Src, $loadFolder)) -Destination $ModDir -PassThru

# Copy supporting libraries
$destination = ([System.IO.Path]::Combine($ModDir, $common, $assemblies, $RwUtil))
New-Item -ItemType File -Path $destination -Force
Copy-Item "$Src\..\references\$RwUtil" -Destination $destination -Force -PassThru

# Copy defs folder
Copy-Item -Path ([System.IO.Path]::Combine($Src, $defs)) -Destination ([System.IO.Path]::Combine($ModDir, $common, $defs)) -Recurse -PassThru

# Copy Languages folder
Copy-Item -Path ([System.IO.Path]::Combine($Src, $languages)) -Destination ([System.IO.Path]::Combine($ModDir, $common, $languages)) -Recurse -PassThru

# Copy Patches folder
Copy-Item -Path ([System.IO.Path]::Combine($Src, $patches)) -Destination ([System.IO.Path]::Combine($ModDir, $common, $patches)) -Recurse -PassThru

# Copy Textures folder
Copy-Item -Path ([System.IO.Path]::Combine($Src, $texture)) -Destination ([System.IO.Path]::Combine($ModDir, $common, $texture)) -Recurse -PassThru

# Zip the directory
$dir = New-Object -TypeName System.IO.DirectoryInfo -ArgumentList $ModDir
Compress-Archive -Path "$ModDir\*" -DestinationPath ".\$($dir.Name).zip"
