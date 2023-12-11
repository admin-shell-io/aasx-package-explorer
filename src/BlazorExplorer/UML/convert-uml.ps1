param (
	[switch]$help = $false,
	[switch]$view = $false,
	[string]$file = ""
)

if ( $help )
{
	$hlptext = @"
This script converts .puml files to .svg files.
It requires a docker-container named 'dstockhammer/plantuml'
which implements PlantUML. 
See: https://hub.docker.com/r/dstockhammer/plantuml
For syntax of PlantUML see: https://plantuml.com/
Syntax: convert-uml [-help] [-view] [name of .puml file WITHOUT extension]
Flags:
  -help		Show this help
  -view     After converting each file, start a brower to show that file
(c) 2023 by Michael Hoffmeister, HKA
"@
	
	Write-Host $hlptext
	Exit
}

function ConvertSingleFile {
	param ($file)

	# filenames
	$src = "${file}.puml"
	$dst = "${file}.svg"
	Write-Host "Converting single source $src -> $dst .."

	# convert
	$pwd = Get-Location
	$cmd = "& docker run  -v ${pwd}:/data dstockhammer/plantuml -tsvg $src"
	Write-Host Execute: $cmd
	Invoke-Expression $cmd

	# view?
	if ($view)
	{
		Write-Host Viewing $dst ..
		$cmd2 = "& start $dst"
		Invoke-Expression $cmd2
	}

	Write-Host .
}

if ( $file )
{
	ConvertSingleFile $file
} 
else
{
	Write-Host "Converting all files *.puml in ${Get-Location} .."
	$files = Get-ChildItem *.puml
	foreach ($fullfile in $files) {
		$file = [System.IO.Path]::GetFileNameWithoutExtension("$fullfile")
		ConvertSingleFile $file
	}
}