# Building the Solution

We provide PowerShell scripts to help you install the dependencies and build 
the solution from the command line.

**src**. First, change to the `src/` directory. All the subsequent scripts will be 
invoked from there.

**Dependencies**. We separated *development* dependencies, which are installed for many different
solutions (such as Visual Studio Build tools) and *solution* dependencies
which are specific to this particular solution.

To install the development dependencies (for example, on a virtual machine), 
run:

```powershell
.\InstallDevDependencies.ps1
```

To install the tools for build-test-inspect workflow, call:

```powershell
.\InstallToolsForBuildTestInspect.ps1
```

and to install the tools for appearance checks, run:

```powershell
.\InstallToolsForStyle.ps1
```

The dependencies of the solution are installed by:

```powershell
.\InstallBuildDependencies.ps1
```

**Build**. Now you are all set to build the solution:

```powershell
.\Build.ps1
```
