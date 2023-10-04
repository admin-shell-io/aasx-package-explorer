# wix
wix standalone - checkout this repo to build wix project inside github action

## Workflow example

```yaml
name: .NET Core Build

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:

    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v2
    - name: Install Wix
      uses: actions/checkout@v2
      with:
        repository: fbarresi/wix
        path: wix
    - name: Setup .NET Core
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 3.1.101
    - name: Install dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --configuration Release --no-restore
    - name: Test
      run: dotnet test --no-restore --verbosity normal
    - name: Build Setup
      run: |
        wix\tools\candle.exe Product.wxs -o obj\ -ext WixUtilExtension -ext WixUIExtension
        wix\tools\light.exe obj\*.wixobj -o bin\Installer.msi -ext WixUtilExtension -ext WixUIExtension -dWixUILicenseRtf="License.rtf"
    - name: Create Release
      id: create_release
      uses: actions/create-release@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        tag_name: 1.0.${{ github.run_number }}
        release_name: 1.0.${{ github.run_number }}
        draft: false
        prerelease: false
    - name: Upload Installer
      id: upload-release-asset 
      uses: actions/upload-release-asset@v1
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      with:
        upload_url: ${{ steps.create_release.outputs.upload_url }} # This pulls from the CREATE RELEASE step above, referencing it's ID to get its outputs object, which include a `upload_url`. See this blog post for more info: https://jasonet.co/posts/new-features-of-github-actions/#passing-data-to-future-steps 
        asset_path: bin\Installer.msi
        asset_name: Installer_v1.0.${{ github.run_number }}.msi
        asset_content_type: application/x-msi

```
