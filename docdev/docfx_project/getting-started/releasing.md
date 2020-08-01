# Releasing

## Versioning

We version based on the date of the release (*e.g.*, `19-10-21`).
The pre-releases are marked with `pre` (*e.g.*, `19-10-21.pre3`) and 
post-releases with `post` (*e.g.*, `19-10-21.post2`). 

While we admit that such a versioning scheme is uninformative with respect to 
number of new features, bugs fixed or critical changes, we found it too hard to 
come up with a versioning scheme for a GUI program such as ours that would be 
neither misleading nor confusing. Schemes like [semantic versioning](
https://semver.org) work well for libraries or command-line tools where breaking
changes and extensions are well-defined. However, a breaking change in a GUI is
not as easily defined and much more subjective (*e.g.*, a breaking change for 
one user is a minor improvement to another user).

## Artefacts

Before you can release, you need the following artefacts:
1) Solution release build
2) AASX samples and
3) ecl@ss definitions.

Once these are set, you can package the release.

### Build Solution for Release

To build the solution for release, invoke:

```powershell
.\src\BuildForRelease.ps1
```

If you want to clean a previous build, call:

```powershell
.\src\BuildForRelease.ps1 -clean
```

This will produce the solution build in `artefacts/` directory.

### AASX Samples

Tha AASX samples live at http://admin-shell-io.com/samples/. They are 
intentionally not included in the repository in order to avoid mangaing multiple
sites.

To download the samples to their expected directory, invoke:

```powershell
.\src\DownloadSamples.ps1
```

### Ecl@ss Definitions

We can not check in ecl@ss definitions due to their restrictive license, but we
need to include them in the release. Hence you need to manually copy them to 
`eclass/` directory.

### Package

The release is now ready to be packaged. Call:

```powershell
.\src\PackageRelease.ps1
```

Multiple release packages will be produced (small, with or without ecl@ss 
*etc*.). 

You can now go to Github, create a release and attach the files.