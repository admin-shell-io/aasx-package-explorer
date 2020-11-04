# Releasing

## Versioning

We version based on the date of the release (*e.g.*, `2019-10-21`).
The suffixes `alpha` and `beta` indicate the testing maturity of the release: 
* `Alpha` releases have not been manually tested (only automatic tests were 
  performed).
* `Beta` releases underwent only a bit of manual testing, but no thorough manual
  testing has been performed.
* A release without suffix implies that a couple of users tested the release and
  were satisfied with the quality of the software.

While we admit that such a versioning scheme is uninformative with respect to 
number of new features, bugs fixed or critical changes, we found it too hard to 
come up with a versioning scheme for a GUI program such as ours that would be 
neither misleading nor confusing. Schemes like [semantic versioning](
https://semver.org) work well for libraries or command-line tools where breaking
changes and extensions are well-defined. However, a breaking change in a GUI is
not as easily defined and much more subjective (*e.g.*, a breaking change for 
one user is a minor improvement to another user).

## Build Solution for Release

To build the solution for release, invoke:

```powershell
.\src\BuildForRelease.ps1
```

If you want to clean a previous build, call:

```powershell
.\src\BuildForRelease.ps1 -clean
```

This will produce the solution build in `artefacts/` directory.

In cases of substantial changes to the solution (*e.g.*, conversion of the
projects from legacy to SDK style), you need to delete `bin` and `obj` 
subdirectories beneath `src` as dotnet (and consequently MSBuild) will not do 
that for you. We provide a shallow script to save you a couple of 
keystrokes:

```powershell
.\src\RemoveBinAndObj.ps1
```

## Package the Release

The release is now ready to be packaged. Call `PackageRelease.ps` with the
desired version:

```powershell
.\src\PackageRelease.ps1 -version 2020-08-14.alpha
```

Multiple release packages will be produced (with web browser integrated, small 
*etc*.).