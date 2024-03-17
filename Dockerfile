FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
# donet restore has issues when ran from the root directory in the docker container with the source code (scan related?)
# so we only copy the project solution files over and then restore.
COPY *.sln .
COPY *.csproj .
RUN dotnet restore
# then we copy the rest of the source and build.
COPY . .
COPY settings.template.json /app
COPY labels.gitlab.template.json /app
COPY labels.forgejo.template.json /app
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "LabelSync.dll"]
