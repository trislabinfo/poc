# Build stage - Alpine SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy entire server directory structure (maintainable - automatically includes all projects)
# This includes the solution file at server/Datarizen.sln
COPY ["server/", "server/"]

# Restore dependencies using solution file (automatically resolves all project references)
RUN dotnet restore "server/Datarizen.sln" --verbosity quiet

# Build & publish
WORKDIR "/src/server/src/ApiGateway"
RUN dotnet publish "ApiGateway.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage - Alpine Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install dependencies
RUN apk add --no-cache \
    curl \
    icu-libs \
    tzdata

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser

# Copy app
COPY --from=build /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
