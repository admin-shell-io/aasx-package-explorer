# Continuous Integration

To establish confidence in the software as well as to continuously maintain 
the code quality, we provide scripts to run pre-merge checks on your local 
machine as well as Github workflows to run remotely.

## Running Checks Locally

To run all the checks:

```powershell
.src\Check.ps1
```

To format the code in-place with `dotnet-format`:

```powershell
.\src\FormatCode.ps1
```

We use [doctest-csharp](
https://github.com/mristin/doctest-csharp
) for [doctests](
https://en.wikipedia.org/wiki/Doctest). To extract the doctests and generate 
the corresponding unit tests:

```powershell
.\src\Doctest.ps1
```

To run all the unit tests:

```powershell
.\src\Test.ps1
```

## Github Workflows

Github Actions allow for running continuous integration on Github servers.
For a general introduction, see [Github's documentation on Actions](
https://docs.github.com/en/actions
).

The specification of our Github workflows can be found in [`.github/workflows`](
https://github.com/admin-shell-io/aasx-package-explorer/tree/master/.github/workflows
) directory. Please see the corresponding `*.yml` files for more details.