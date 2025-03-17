@echo off
cd ..
pushd AAEmu.Login
    start /I dotnet build AAEmu.Login.csproj
popd
