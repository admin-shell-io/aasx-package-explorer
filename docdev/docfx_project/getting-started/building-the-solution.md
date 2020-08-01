# Building the Solution

We provide PowerShell scripts to help you install the dependencies and build 
the solution from the command line.

**Dependencies**. We separated *development* dependencies, which are installed for many different
solutions (such as Visual Studio Build tools) and *solution* dependencies
which are specific to this particular solution.

To install the development dependencies (for example, on a virtual machine), 
run:

```powershell
.\src\InstallDevDependencies.ps1
```

To install the tools for build-test-inspect workflow, call:

```powershell
.\src\InstallToolsForBuildTestInspect.ps1
```

and to install the tools for appearance checks, run:

```powershell
.\src\InstallToolsForStyle.ps1
```

The dependencies of the solution are installed by:

```powershell
.\src\InstallBuildDependencies.ps1
```

**Build**. Now you are all set to build the solution for debugging and testing:

```powershell
.\src\BuildForDebug.ps1
```

**Clean**. If you want to clean up the build, call:

```powershell
.\src\BuildForDebug.ps1 -configuration Debug -clean
```
