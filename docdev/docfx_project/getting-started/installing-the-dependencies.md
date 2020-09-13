# Installing the Dependencies

We provide PowerShell scripts to help you install the dependencies and build 
the solution from the command line.

**Tools for development**. Most of the contributors use Visual Studio 
(*e.g.* Visual Studio 2019 Community Edition), the go-to tool for C# and .NET 
development. If you are familiar with it, we would
highly recommend you to use Visual Studio as well. 

Visual Studio comes with a set of build tools (Visual Studio Build Tools) and
package manager (NuGet) included, so you do not need to install them yourself. 

However, if you want to set up a custom build server or develop using 
a custom editor (*e.g.*, Vim) you can use the following script to install 
development tools such as Visual Studio Build Tools, NuGet *etc*.:

```powershell
.\src\InstallToolsForDevelopment.ps1
```

Mind that you need admin privileges in order to install these development tools.

The script also provides an option for a dry run to list the tools without
actually installing anything on the system (and does not require any admin
privileges): 

```powershell
.\src\InstallToolsForDevelopment.ps1 -DryRun
```
**Solution Dependencies**. The solution relies on many solution-specific tools
(such as a tool for code formatting) as well as third-party libraries.

The solution dependencies are split into three different categories:

* Tools for the build-test-inspect workflow (such as Resharper CLI),
* Tools necessary to check the conformance of the code to the style guideline
  (such as dotnet-format),
* The third-party libraries (using NuGet) and
* Sample AASX packages used for integration testing (provided at 
  http://admin-shell-io.com/samples/).

The following script installs all the dependencies in one go:

```
.\src\InstallSolutionDependencies.ps1
```

This script *should not* require any admin privileges.

We also provide a script for each respective category which is called from
within `src\InstallSolutionDependencies.ps1`:
* `src\InstallToolsForBuildTestInspect.ps1`
* `src\InstallToolsForStyle.ps1`,
* `src\InstallBuildDependencies.ps1` and
* `src\DownloadSamples.ps1`.

The logic had to be separated so that individual workflows of the continuous 
integration would have only the dependencies installed that they actually need.

This script *should not* require any admin privileges.

**Updating the dependencies**. Whenever the dependencies change, the install
script needs to be re-run:

```
.\src\InstallSolutionDependencies.ps1
```

Obsolete dependencies will not be removed (*e.g.*, unused NuGet packages in
`packages/` or AASX samples). You need to manually remove them.
