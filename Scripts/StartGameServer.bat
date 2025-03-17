@echo off
cd ..
pushd AAEmu.Game
    start /I dotnet build AAEmu.Game.csproj
popd
