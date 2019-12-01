@echo off
pushd Worker\bin\Debug\netcoreapp3.0
dotnet.exe worker.dll
popd