#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["AzReplicate.Sample.AzBlobSourcerer/AzReplicate.Sample.AzBlobSourcerer.csproj", "AzReplicate.Sample.AzBlobSourcerer/"]
RUN dotnet restore "AzReplicate.Sample.AzBlobSourcerer/AzReplicate.Sample.AzBlobSourcerer.csproj"
COPY . .
WORKDIR "/src/AzReplicate.Sample.AzBlobSourcerer"
RUN dotnet build "AzReplicate.Sample.AzBlobSourcerer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzReplicate.Sample.AzBlobSourcerer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AzReplicate.Sample.AzBlobSourcerer.dll"]