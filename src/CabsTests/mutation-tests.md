dotnet tool install -g dotnet-stryker
TZ=Europe/Warsaw DiffEngine_Disabled=true dotnet-stryker -p Cabs.csproj -m '**/Transit.cs' -m '**/TransitDto.cs'
