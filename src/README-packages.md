# Overview on files & directories in the build directory

## ready to run packages

* portable-restricted-eclass          (often not available, restricted code & data such as eclass tables)
* portable-restricted                 (sometimes not avilable, contains restricted code)
* portable-open                       (open codes, including CEFsharp web browser plugin)
* portable-small                      (minimal plugins, no embedded web browser)

Each package provides:
* AasxPackageExplorer \ AasxPackageExplorer.exe -> double click to start the PackageExplorer
* sample-aasx \ *.aasx                          -> sample AASX files to drag&drop into the PackageExplorer

## Plugin directories

* the plug-ins are sorted into different directories, each starting with plugins-*

## AasxPackageExplorer

* contains the "pure" files for the main application only
* .\eclass\ would contain ECLASS data files
* .\plugins\ would contain plugins (see above)
* AasxPackageExplorer.options.json contains the default options, whenever AasxPackageExplorer.exe is being loaded
  (exception: command line arguments provided)