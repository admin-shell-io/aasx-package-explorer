﻿@echo off
echo WARNING!
echo ========
echo This file is to be executed in a the data sub-folder of AasxGenerate.exe. It will copy external data to this folder
pause
@echo on
echo 
copy ..\..\..\..\..\..\aasx-sample-data\AasxGenerate_data\*.* .
copy ..\..\..\..\..\..\aasx-sample-data\AasxGenerate_usb\*.* .
