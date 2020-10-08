# API Documentation

This is automatically-generated documentation of the API extracted
from the structured comments in the code base.

## Documentation Quality
We are aware that the current generated documentation might be confusing or 
overwhelming for the new developers since it still lacks structure and no entry
points are clearly visible. 

At the moment (August 2020), we are working on an introductory document to
give you an overview of the code structure and how individual parts fit 
together.

## Entry Point

Current entry point to the program is: [AasxPackageExplorer.App](
AasxPackageExplorer.App.yml
). This class parses the command-line arguments and instantiates 
the [AasxPackageExplorer.MainWindow](AasxPackageExplorer.MainWindow.yml).

The graphical user interface (GUI) is implemented in the namespace 
[AasxPackageExplorer](AasxPackageExplorer.yml).

## Data Model

The data model of the AASX lives in the [AdminShellNS](AdminShellNS.yml) 
namespace. Any changes and extensions to the data model should go there.

Since the data model is still evolving (August 2020), we put the code 
responsible for managing the legacy data models (*e.g.*, AASX V1.0) to 
the namespace [AasxCompatibilityModels](AasxCompatibilityModels.yml).

The pre-defined concepts (such as [VDI 2770](
https://www.vdi.de/richtlinien/details/vdi-2770-blatt-1-betrieb-verfahrenstechnischer-anlagen-mindestanforderungen-an-digitale-herstellerinformationen-fuer-die-prozessindustrie-grundlagen
)) reside in the namespace 
[AasxPredefinedConcepts](AasxPredefinedConcepts.yml).

## Package Signatures

Package signatures (*e.g.*, using [X.509](https://en.wikipedia.org/wiki/X.509) 
standard) are handled in the namespace [AasxSignature](AasxSignature.yml).

## Clients

AASX Package Explorer uses various clients to communicate with the external
sources. The clients are compartmentalized in the individual `*Client` 
namespaces (*e.g.*, [AasxMqttClient](AasxMqttClient.yml) and 
[AasxOpenIdClient](AasxOpenIdClient.yml)).

## Imports and Exports

Imports and exports from different data formats are handled in `*ImExport` 
namespaces such as [AasxAmlImExport](AasxAmlImExport.yml) and
[AasxUANodesetImExport](AasxUANodesetImExport.yml).

## Plug-ins

AASX Package Explorer uses plug-ins to reduce the size of the different 
binary releases. For example, a plug-in web browser 
([AasxPluginWebBrowser](AasxPluginWebBrowser.yml)) is pretty heavy in size 
(about 60 Mb), but many users do not actually need it. Hence they can download
a much skinnier release without the browser (only about 2 Mb).

The namespace [AasxIntegrationBase](AasxIntegrationBase.yml) provides all the
functionality needed to integrate the plug-ins.

## AasxToolkit Program

Apart from the main AASX Package Explorer, we include an additional 
program, [AasxToolkit](AasxToolkit.yml), to generate and manipulate AASX 
packages from the command line. This program is also a good entry point if you
want to see how you can perform operations on AASX in code (rather than using
a GUI tool).

## What is a REST Server Doing Here?

It might seem confusing that we include a REST server in the application (
[AasxRestConsoleServer](AasxRestConsoleServer.yml) and 
[AasxRestServerLibrary](AasxRestServerLibrary.yml)). The rationale behind this
inclusion is to make the demonstrations and local tests simple & easy.

However, from the software engineering point of view, this code does not really
belong to the Package Explorer and should be refactored out of it. We are 
currently (August 2020) working on packaging [AASX Server](
https://github.com/admin-shell-io/aasx-server) in such a way that AASX Package
Explorer can use it directly as a dependency (*e.g.*, as a NuGet package).
As soon as we packaged the AASX Server, we will remove these server-related
bits from this code base.
