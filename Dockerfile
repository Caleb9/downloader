#
# Back-end build
#
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /source/Api

# copy source code and restore as distinct layers
COPY Api/Api.csproj ./
RUN dotnet restore  \
        -r linux-musl-x64

COPY Api/. ./
# add read permission to appsettings.json so the container can be run as non-root (with --user $UID:$GID option)
# and build the app
RUN chmod 644 ./appsettings.json && \
    dotnet publish                  \
        -c release                  \
        -o /app                     \
        -r linux-musl-x64           \
        --self-contained false      \
        --no-restore


#
# Front-end build
#
FROM node:20 AS client-build
WORKDIR /app

ENV PATH /app/node_modules/.bin:$PATH

COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/. ./
RUN npm run build


#
# final stage/image
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN mkdir -p /data/completed /data/incomplete && \
    chmod 777 /data/completed /data/incomplete

WORKDIR /downloader/client
COPY --from=client-build /app/build ./
# Prevent UnauthorizedAccessException when running container as non-root user
RUN chmod 644 ./manifest.json
    
WORKDIR /downloader/Api
COPY --from=api-build /app ./

VOLUME ["/data"]

ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["./Api",                                            \
            "--DownloadDirectories:Incomplete=/data/incomplete",\
            "--DownloadDirectories:Completed=/data/completed"]

EXPOSE 80
