# AASX Package Explorer

![Build-test-inspect](
https://github.com/admin-shell-io/aasx-package-explorer/workflows/Build-test-inspect/badge.svg
) ![Check-style](
https://github.com/admin-shell-io/aasx-package-explorer/workflows/Check-style/badge.svg
) ![Check-commit-messages](
https://github.com/admin-shell-io/aasx-package-explorer/workflows/Check-commit-messages/badge.svg
) ![Generate-docdev](
https://github.com/admin-shell-io/aasx-package-explorer/workflows/Generate-docdev/badge.svg
) [![Coverage Status](
https://coveralls.io/repos/github/admin-shell-io/aasx-package-explorer/badge.svg?branch=master
)](
https://coveralls.io/github/admin-shell-io/aasx-package-explorer?branch=master
)

[![TODOs](
https://admin-shell-io.github.io/aasx-package-explorer/todos/badges/TODOs.svg
)](
https://github.com/admin-shell-io/aasx-package-explorer/blob/gh-pages/todos/task-list/task-list-by-file.md
) [![BUGs](
https://admin-shell-io.github.io/aasx-package-explorer/todos/badges/BUGs.svg
)](
https://github.com/admin-shell-io/aasx-package-explorer/blob/gh-pages/todos/task-list/task-list-by-file.md
) [![HACKs](
https://admin-shell-io.github.io/aasx-package-explorer/todos/badges/HACKs.svg
)](
https://github.com/admin-shell-io/aasx-package-explorer/blob/gh-pages/todos/task-list/task-list-by-file.md
)

AASX Package Explorer is a C# based viewer / editor for the 
Asset Administration Shell.

![screenshot](
https://github.com/admin-shell-io/aasx-package-explorer/raw/master/screenshot.png
)

To help you familiarize with the concept of Asset Administration Shell 
we provide the screencasts (both in English and German) at: 
http://admin-shell-io.com/screencasts/.

For further information about the Asset Administration Shell, see the 
publication [Details of the Asset Administration Shell](
https://www.plattform-i40.de/PI40/Redaktion/EN/Downloads/Publikation/Details-of-the-Asset-Administration-Shell-Part1.html
) by Platform I4.0.

We provide a couple of sample admin shells (packaged as .aasx) for you to 
test and play with the software at:
http://www.admin-shell-io.com/samples/

## Installation

We provide the binaries for Windows 10 in [the releases](
https://github.com/admin-shell-io/aasx-package-explorer/releases). 

## Issues

If you want to request new features or report bugs, please 
[create an issue](
https://github.com/admin-shell-io/aasx-package-explorer/issues/new/choose). 

## Contributing

The latest documentation for developers is available [on this page](
https://admin-shell-io.github.io/aasx-package-explorer/devdoc/
).

If you want to contribute in code, see [Section "Getting started"](
https://admin-shell-io.github.io/aasx-package-explorer/devdoc/getting-started/intro.html
).

## Other Open Source Implementations of AAS

At the time of this writing (2020-08-14), we are aware of the following related
implementations of asset administration shells (AAS):

* **BaSyx** (https://projects.eclipse.org/projects/technology.basyx) provides
  various modules to cover a broad scope of Industrie 4.0 (including AAS).
  Hence its substantially more complex architecture. 
  
* **PyI40AAS** (https://git.rwth-aachen.de/acplt/pyi40aas) is a Python 
  module for manipulating and validating AAS. 
  
* **SAP AAS Service** (https://github.com/SAP/i40-aas) provides a system based
  on Docker images implementing the RAMI 4.0 reference architecture (including
  AAS).

While these projects try to implement a wider scope of programatic features,
AASX Package Explorer, in contrast, is a tool with graphical user interface 
meant for experimenting and demonstrating the potential of asset administration
shells targeting tech-savvy and less technically-inclined users alike.
