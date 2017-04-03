#!/bin/sh
export artifacts=$(dirname "$(readlink -f "$0")")/artifacts
export configuration=Release

dotnet restore NLog.StructuredLogging.Json.sln --verbosity minimal || exit 1
dotnet build src/NLog.StructuredLogging.Json/NLog.StructuredLogging.Json.csproj --output $artifacts --configuration $configuration --framework "netstandard1.3" || exit 1
dotnet test src/NLog.StructuredLogging.Json.Tests/NLog.StructuredLogging.Json.Tests.csproj --output $artifacts --configuration $configuration --framework "netcoreapp1.1" || exit 1
