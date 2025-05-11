# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy solution file vào container
COPY . .
RUN dotnet restore EMS_BE.sln
# Build and publish the application
RUN dotnet publish EMS_BE.sln -c Release -o /app/out

# Stage 2: Setup runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "EMS_BE.dll"]
