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

You can also build a single project. 
This is practical when you want to manually test something and do not want to
waste time on MSBuild inspecting which projects need to be rebuilt:

```powershell
.\src\BuildForDebug.ps1 -Project AasxToolkit
```

To clean the build, call:
```powershell
.\src\BuildForDebug.ps1 -clean
```

In cases of substantial changes to the solution (*e.g.*, conversion of the
projects from legacy to SDK style), you need to delete `bin` and `obj` 
subdirectories beneath `src` as dotnet (and consequently MSBuild) will not do 
that for you. We provide a shallow script to save you a couple of 
keystrokes:

```powershell
.\src\RemoveBinAndObj.ps1
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

Please see the source code of `src\Check.ps1` for more details.

### Testing Locally with `Test.ps1`

Unit tests are part of our continuous integration. While you usually test
with `src\Check.ps1` at the end of the development cycle, it is often practical
to continuously test while developing. 

The script `src\Test.ps1` handles all the unit testing for you. 

Please note that you have to build the solution for debug before *each run* 
(using `src\BuildForDebug.ps1`). `src\Test.ps1` will look for binaries in 
`artefacts` directory, and ignore any other binaries you might have built with
your IDE (*e.g.*, building from within Visual Studio will have *no* effect on
`src\Test.ps1` execution!).

To run all the tests:

```powershell
.\src\Test.ps1
```

To list the tests without executing them:

```powershell
.\src\Test.ps1 -Explore
```

To execute an individual unit test:

```powershell
.\src\Test.ps1 -Test AasxDictionaryImport.Cdd.Tests.Test_Context.Empty
```

You can also specify a prefix so that all the corresponding test cases will be
executed:

```powershell
.\src\Test.ps1 -Test AasxDictionaryImport.Cdd
```

### Testing Locally with Visual Studio

Our tests are written using NUnit3, so you need to install NUnit 3 Test Adapter
for Visual Studio (see [this article][nunit3-test-adapter] from NUnit 
documentation).

See [this article][nunit3-test-adapter-usage] for how to use NUnit 3 Test 
Adapter in more detail.

Once the adapter has been installed, you can run all the tests which do not
require any external dependencies (such as AASX samples downloaded from 
http://admin-shell-io.com/samples/).

Our tests with external dependencies use environment variables to specify the
location of the dependencies. While `src\Check.ps1` and related scripts set up
the expected locations in the environment automatically, you need to adjust your
test setting accordingly.

So far, we need to set `SAMPLE_AASX_DIR` environment variable to point to the
absolute path where AASX samples have been downloaded (using 
`src\DownloadSamples.ps1`). This is by definition 
`{your repository}\sample-aasx`.

In order to reflect this environment variable in your test adapter, you have
to create a RUNSETTINGS file and include it into the solution as user-specific.
Please follow [this page][visual-studio-runsettings] from Visual Studio 
documentation how to set up a RUNSETTINGS file.

Here is an example RUNSETTINGS file:

```
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
 <EnvironmentVariables>
    <SAMPLE_AASX_DIR>
        C:\admin-shell-io\aasx-package-explorer\sample-aasx
    </SAMPLE_AASX_DIR>
  </EnvironmentVariables>
</RunSettings>
```

[nunit3-test-adapter]: https://docs.nunit.org/articles/vs-test-adapter/Adapter-Installation.html
[nunit3-test-adapter-usage]: https://docs.nunit.org/articles/vs-test-adapter/Usage.html
[visual-studio-runsettings]: https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file?view=vs-2019

### GUI Tests

Analogous to unit tests, we provide GUI tests (a.k.a. function tests) based on 
[FlaUI](https://github.com/FlaUI/FlaUI).
Such test projects are marked with `.GuiTests` suffix. 

Mind that the tests run against the executables released in 
`artefacts\build\Debug` directory. This means that building the executables
in your IDE will have no effect on GUI tests (unless you manually set the target
for the binaries to `artefacts\build\Debug`). Please always double-check that
you built your binaries with `src\BuildForDebug.ps1` to avoid unnecessary
confusion.

Some of the GUI tests, similarly to unit tests, depend on sample AASX files
which need to be downloaded with `src\DownloadSamples.ps1` before the tests can
run. Once you built for debug and obtained the samples, you are all set to 
automatically test the GUIs.

To run all the GUI tests, call:

```powershell
.\src\TestGui.ps1
```

Similar to unit tests, you can list the tests:

```powershell
.\src\Test.ps1 -Explore
```

, execute a single test:

```powershell
.\src\TestGui.ps1 `
    -Test AasxPackageExplorer.GuiTests.TestBasic.Test_application_start
```

or a group of tests sharing a common prefix: 

```powershell
.\src\TestGui.ps1 `
    -Test AasxPackageExplorer.GuiTests
```

GUI tests are not executed automatically on the remote 
CI servers. First, it is more often than difficult to run GUI tests on headless
remote servers, so it would put a high maintenance burden. Second, GUI tests
take too long and depend on timeouts which are often not controllable
and reproducible enough. In particular, timeouts depend on the server load and
hence can vary greatly. 

## Github Workflows

Github Actions allow for running continuous integration on Github servers.
For a general introduction, see [Github's documentation on Actions](
https://docs.github.com/en/actions
).

The specification of our Github workflows can be found in [`.github/workflows`](
https://github.com/admin-shell-io/aasx-package-explorer/tree/master/.github/workflows
) directory. Please see the corresponding `*.yml` files for more details.