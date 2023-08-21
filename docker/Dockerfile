# Stage 1: Downloader
FROM debian:12-slim AS downloader
RUN apt-get update && apt-get install -y curl unzip
RUN curl -LO https://github.com/Nova-Atomic/Rimworld-Together/releases/latest/download/LinuxX64.zip
RUN unzip LinuxX64.zip -d /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime
WORKDIR /app
COPY --from=downloader /app .

# Set the working directory to /app/data (so we can expose the entire directory later on)
WORKDIR /app/data

# We only need TCP
EXPOSE 25555/tcp

# Entry
ENTRYPOINT ["dotnet", "/app/GameServer.dll"]