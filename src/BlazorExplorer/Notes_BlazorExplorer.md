# Notes for BlazorExplorer

This file holds notes specific for the new BlazorExplorer. It depends on the 
(old) BlazorUI but shall develop more towards a HTML-version of 
PackageExplorer. Therefore design goals:

* refacture BlazorUI, better structure & code quality
* top-level menue ressembling PackageExplorer
* file-operations similar to local PC
* repository-handling visible
* editing capabilities
* device fit = PC

## TODO

* cut-copy does not refresh screen?
* save repo
* DPDM Nameplate convert into new version: crash
* Edit script / JSON clipboard

## Notes

* Project reference to AasxIntegrationBaseGdi is required, although only
  loaded by plugins. Else: "file / assembly ImagMagick-Q8..dll could not be found"