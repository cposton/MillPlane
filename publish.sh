#!/usr/bin/env zsh

echo "Publishing for macOS..."
dotnet publish -r osx-x64 -c Release --self-contained -p:"PublishSingleFile=true;IncludeNativeLibrariesForSelfExtract=true"

echo "Publishing for Windows..."
dotnet publish -r win-x64 -c Release --self-contained -p:"PublishSingleFile=true;IncludeNativeLibrariesForSelfExtract=true"
