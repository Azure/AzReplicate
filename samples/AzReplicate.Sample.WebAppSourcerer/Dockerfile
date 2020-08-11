#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["AzReplicate.Sample.WebAppSourcerer/AzReplicate.Sample.WebAppSourcerer.csproj", "AzReplicate.Sample.WebAppSourcerer/"]
RUN dotnet restore "AzReplicate.Sample.WebAppSourcerer/AzReplicate.Sample.WebAppSourcerer.csproj"
COPY . .
WORKDIR "/src/AzReplicate.Sample.WebAppSourcerer"
RUN dotnet build "AzReplicate.Sample.WebAppSourcerer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzReplicate.Sample.WebAppSourcerer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzReplicate.Sample.WebAppSourcerer.dll"]