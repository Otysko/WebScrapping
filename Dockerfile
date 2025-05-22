# This stage is used when running from VS in fast mode
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base


# Install required dependencies and tools
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    curl \
    xvfb \
    firefox-esr \
    libnss3 \
    libxss1 \
    libasound2 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libgbm1 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    libgtk-3-0 \
    libglib2.0-0 \
    fonts-liberation \
    libappindicator3-1 \
    libpango1.0-0 \
    libnss3-dev \
    libxcursor1 \
    libxi6 \
    libgconf-2-4 \
    lsb-release \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

# Install latest Geckodriver (for Firefox)
RUN GECKODRIVER_VERSION=$(curl -s https://api.github.com/repos/mozilla/geckodriver/releases/latest | grep 'tag_name' | cut -d\" -f4) \
    && wget -O /tmp/geckodriver.tar.gz https://github.com/mozilla/geckodriver/releases/download/$GECKODRIVER_VERSION/geckodriver-$GECKODRIVER_VERSION-linux64.tar.gz \
    && tar -xzf /tmp/geckodriver.tar.gz -C /usr/local/bin/ \
    && chmod +x /usr/local/bin/geckodriver \
    && rm /tmp/geckodriver.tar.gz

USER app
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WebScrappingTrades.csproj", "."]
RUN dotnet restore "./WebScrappingTrades.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./WebScrappingTrades.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebScrappingTrades.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebScrappingTrades.dll"]