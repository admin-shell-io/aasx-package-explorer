# Troubleshooting

## "Execution of scripts is disabled on this system."

If the [Execution Policy](
https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_execution_policies
) of your system is not set to `Unrestricted`, you
might not be able to directly execute the build scripts like `Build.ps1`.
Instead you have to use PowerShell's `-ExecutionPolicy Bypass` option.

*Example error:*

```
> .\Build.ps1
.\Build.ps1 : Die Datei ".\Build.ps1" kann nicht geladen werden, 
da die Ausführung von Skripts auf diesem System deaktiviert ist. 
Weitere Informationen finden Sie unter "about_Execution_Policies" 
(https:/go.microsoft.com/fwlink/?LinkID=135170).
In Zeile:1 Zeichen:1
+ .\Build.ps1
+ ~~~~~~~~~~~
    + CategoryInfo          : Sicherheitsfehler: (:) [], PSSecurityException
    + FullyQualifiedErrorId : UnauthorizedAccess
```

*Workaround:*

```
> powershell -ExecutionPolicy Bypass -File .\Build.ps1
```

## Unauthorized Access by NuGet

If calls to NuGet or dotnet (for example in `InstallBuildDependencies.ps1`)
fail with an Unauthorized Access error, you might have to configure NuGet to
use your proxy.  You can do so by setting NuGet's `http_proxy` and, if
necessary, `http_proxy.user` config options.

*Workaround:*

```
> nuget.exe config -set http_proxy=http://<PROXY>
> nuget.exe config -set http_proxy.user=<DOMAIN>\<USER>
```

## Using Visual Studio's MSBuild executable

If you have a Visual Studio installation on your machine, you can use Visual
Studio's MSBuild executable instead of installing MSBuild using the
`InstallDevDependencies.ps1` script.  The executable is located in the
`MSBuild\Current\Bin` directory of your Visual Studio installation, typically
`C:\Program Files (x86)\Microsoft Visual Studio\<Release>\<Edition>\MSBuild\Current\Bin`.  
Make sure that this directory is listed in your `PATH` environment variable.

## Using rulers in Visual Studio

Please see this Stack Overflow question and answers if you want Visual Studio
to show rulers (*e.g.*, at 120 characters):

https://stackoverflow.com/questions/5887107/ruler-in-visual-studio

In particular, we endorse this answer:

https://stackoverflow.com/a/57904374/1600678