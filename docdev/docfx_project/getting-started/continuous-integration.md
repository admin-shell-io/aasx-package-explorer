# Continuous Integration

To establish confidence in the software as well as to continuously maintain 
the code quality, we provide scripts to run pre-merge checks on your local 
machine as well as Github workflows to run remotely.

All the following scripts *should not* require any administration privilege.

## Building for Debug

To build the solution for debugging and testing, invoke:

```powershell
.\src\BuildForDebug.ps1
```

To clean the build, call:
```powershell
.\src\BuildForDebug.ps1 -clean
```

## Reformatting Code

We use `dontet-format` to automatically fix the formatting of
the code to comply with the style guideline. To reformat the code in-place, 
call:

```powershell
.\src\FormatCode.ps1
```

## Generate Doctests

We use [doctest-csharp](
https://github.com/mristin/doctest-csharp
) for [doctests](
https://en.wikipedia.org/wiki/Doctest). To extract the doctests and generate 
the corresponding unit tests:

```powershell
.\src\Doctest.ps1
```

## Licenses

We maintain one file listing all the copyrights and licenses, `LICENSE.txt`.
This license file needs to be replicated in each of the solution projects.

If there are changes to `LICENSE.txt`, propagate it to the projects by calling
the following script:

`.\src\CopyLicenses.txt`

## Running Checks Locally

We wrote the checks in separate scripts (*e.g.*, `src\CheckFormat.ps1`) and 
bundled them in one script, `src\Check.ps1`. If you want to run all the checks,
simply call:

```powershell
.\src\Check.ps1
```

The script `src\Check.ps1` will inform you which individual scripts were run. In
case of failures, you can just run the last failing script.

For example, if you only want to run the unit tests, call:

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