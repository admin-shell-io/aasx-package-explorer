# Contributing

## Notes on LICENSE.txt

The file `LICENSE.TXT` in the main folder of the repo is the leading license
information, even if it does not show up in the Visual Studio solution. To
update all dependent license files, manually start `src/CopyLicense.ps1`.

## Pull Requests

**Feature branches**. We develop using the feature branches, see this section of the Git book:
https://git-scm.com/book/en/v2/Git-Branching-Branching-Workflows.

If you are a member of the development team, create a feature branch directly
within the repository.

Otherwise, if you are a non-member contributor, fork the repository and create
the feature branch in your forked repository. See [this Github tuturial](
https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork
) for more guidance. 

**Branch Prefix**. Please prefix the branch with your Github user name 
(*e.g.,* `mristin/Add-some-feature`).

**Continuous Integration**. Github will run the continuous integration (CI) automatically through Github 
actions. The CI includes building the solution, running the test, inspecting
the code *etc.* (see below the section "Pre-merge Checks").

Please note that running the Github actions consumes limited resources (we have only 2,000 minutes
per month available for CI on our current Github plan). You can manually disable workflows
by appending the following lines to the body of the pull request:
* `The workflow build-test-inspect was intentionally skipped.`
* `The workflow check-style was intentionally skipped.`.

For an example, see [this pull request](
https://github.com/admin-shell-io/aasx-package-explorer/pull/94
).

## Commit Messages

The commit messages follow the guidelines from 
from https://chris.beams.io/posts/git-commit:
* Separate subject from body with a blank line
* Limit the subject line to 50 characters
* Capitalize the subject line
* Do not end the subject line with a period
* Use the imperative mood in the subject line
* Wrap the body at 72 characters
* Use the body to explain *what* and *why* (instead of *how*)

## Binary Files

We use Git Large File Support (LFS) to handle binary files. Please do not forget
to install Git-Lfs (https://git-lfs.github.com) on your computer.

You need to make sure the files are tracked before you add binaries to the 
repository. Invoking:
```bash
$ git lfs track
```
gives you the list like this one:
```
Listing tracked patterns
    *.png (.gitattributes)
```

## Building the Solution

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

## Pre-merge Checks

We still assume the current directory is `src/`. Besides scripts for building,
we provide scripts to run pre-merge checks on your local machine.

To run all the checks, run:

```powershell
.\Check.ps1
```

To format the code in-place with `dotnet-format`, invoke:

```powershell
.\FormatCode.ps1
```

To run the unit tests:

```powershell
.\Test.ps1
```
