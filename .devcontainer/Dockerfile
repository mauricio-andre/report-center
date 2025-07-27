FROM mcr.microsoft.com/devcontainers/dotnet:9.0

RUN apt-get update && apt-get install -y \
  git \
  curl \
  vim \
  && rm -rf /var/lib/apt/lists/*

RUN dotnet tool install --global dotnet-ef && dotnet dev-certs https
