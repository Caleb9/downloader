FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

# copy source code and restore as distinct layers
COPY . .
WORKDIR /source/Api
RUN dotnet restore  \
        -r linux-musl-x64

# add read permission to appsettings.json so the container can be run as non-root (with --user $UID:$GID option)
RUN chmod 644 ./appsettings.json

# build app
RUN dotnet publish                  \
        -c release                  \
        -o /app                     \
        -r linux-musl-x64           \
        --self-contained false      \
        --no-restore


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine-amd64

RUN mkdir -p /data/completed /data/incomplete && \
    chmod 777 /data/completed /data/incomplete
    
WORKDIR /downloader
COPY --from=build /app ./

VOLUME ["/data"]

ENTRYPOINT ["./Api",                                            \
            "--DownloadDirectories:Incomplete=/data/incomplete",\
            "--DownloadDirectories:Completed=/data/completed"]

EXPOSE 80
