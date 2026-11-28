# Use standard ASP.NET Core 9.0 runtime base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 7860
ENV ASPNETCORE_URLS=http://+:7860

# Use SDK 9.0 image to build and restore
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project csproj files for restoring dependencies
COPY ["NdeefApp/LaundryApi.csproj", "NdeefApp/"]
COPY ["LaundryShared/LaundryShared.csproj", "LaundryShared/"]
COPY ["LaundryDomain/LaundryDomain.csproj", "LaundryDomain/"]
COPY ["LaundryServies/LaundryServies.csproj", "LaundryServies/"]
COPY ["LaundryServiesAbstraction/LaundryServiesAbstraction.csproj", "LaundryServiesAbstraction/"]
COPY ["LaundryPerstince/LaundryPerstince.csproj", "LaundryPerstince/"]

# Restore dependencies
RUN dotnet restore "NdeefApp/LaundryApi.csproj"

# Copy the rest of the source code
COPY . .

# Build the main project
WORKDIR "/src/NdeefApp"
RUN dotnet build "LaundryApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the built app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LaundryApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final production image setup
FROM base AS final
USER root
WORKDIR /app
COPY --from=publish /app/publish .

# Create App_Data folder and ensure proper ownership for non-root runtime safety
RUN mkdir -p /app/App_Data && chown -R app:app /app

USER app
ENTRYPOINT ["dotnet", "LaundryApi.dll"]
