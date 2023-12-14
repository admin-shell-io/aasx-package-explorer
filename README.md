# AASX Package Explorer

![Build-test-inspect](
https://github.com/admin-shell-io/aasx-package-explorer/actions/workflows/build-test-inspect.yml/badge.svg
) ![Check-style](
https://github.com/admin-shell-io/aasx-package-explorer/actions/workflows/check-style.yml/badge.svg
) ![Check-commit-messages](
https://github.com/admin-shell-io/aasx-package-explorer/actions/workflows/check-commit-messages.yml/badge.svg
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

To help you familiarize with the concept of Asset Administration Shell and editing an Asset Administration Shell with the AASX Package Explorer
we provide screencasts (both in English and German) for V2.0 at: 
https://admin-shell-io.com/screencasts/.

For V3.0 (including changes to V2.0) please have a look at the tutorials for the Specifications itself at the [Youtube Channel Industrial Digital Twin](https://www.youtube.com/playlist?list=PLCO0zeX96Ia1hsToD9lRPDMI4P-kbt_CT) 

The basis for the implementatzion are the [Specifications of the Asset Administration Shell](https://industrialdigitaltwin.org/en/content-hub/aasspecifications
) by [IDTA]A(https://industrialdigitaltwin.org).

We provide a couple of sample admin shells (packaged as .aasx) for you to 
test and play with the software at (V2.0):
http://www.admin-shell-io.com/samples/

## Installation

We provide the binaries for Windows 10 in [the releases](
https://github.com/admin-shell-io/aasx-package-explorer/releases). 

(Remark: In special cases you may like to use a current build.
Please click on a green check mark and select "Check-release" details.)

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

*	**NOVAAS** (https://gitlab.com/gidouninova/novaas) provides an implementation
  of the AAS concept by using JavaScript and Low-code development platform (LCDP)
  Node-Red.

* **Java Dataformat Library** (https://github.com/admin-shell-io/java-serializer)
  provides serializer and derserializer for various dataformats as well as the
  creation and validation of AAS, written in Java.

While these projects try to implement a wider scope of programatic features,
AASX Package Explorer, in contrast, is a tool with graphical user interface 
meant for experimenting and demonstrating the potential of asset administration
shells targeting tech-savvy and less technically-inclined users alike.

In 2021 the [Eclipse Digital Twin Top Level Project](https://projects.eclipse.org/projects/dt) 
was created. See sub-projects for more projects featuring digital twins and the Asset Administration Shell.

The AASX Package Explorer also includes an internal REST server and OPC UA
server for the loaded .AASX. Based on this a separate AASX Server is
available (https://github.com/admin-shell-io/aasx-server) which can host
several .AASX simultanously (see example https://admin-shell-io.com/5001/).

