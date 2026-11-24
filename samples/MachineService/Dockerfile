# check=skip=InvalidDefaultArgInFrom

# Copyright 2026 Alexander Stärk
#
#  Licensed under the Apache License, Version 2.0 (the "License");
#  you may not use this file except in compliance with the License.
#  You may obtain a copy of the License at
#
#      http://www.apache.org/licenses/LICENSE-2.0
#
#  Unless required by applicable law or agreed to in writing, software
#  distributed under the License is distributed on an "AS IS" BASIS,
#  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
#  See the License for the specific language governing permissions and
#  limitations under the License.

# Pass the version from BAS_Auth_TargetFramework in Directory.Build.props.
ARG DOTNET_VERSION
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /source
COPY global.json ./
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props ./
COPY README.md ./
COPY src/ ./src/
COPY samples/MachineService/ ./samples/MachineService/
RUN dotnet publish samples/MachineService/Base.Auth.MachineService.csproj -c Release -o /output -p:UseAppHost=false -p:DisableGitVersionTask=true

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}
WORKDIR /app
COPY --from=build /output ./
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Database__ConnectionStrings__SQLite__ConnectionString="Data Source=/data/auth.db"
EXPOSE 7443
USER $APP_UID
ENTRYPOINT ["dotnet", "Basilisque.Base.Auth.MachineService.dll"]
