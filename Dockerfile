#
# Back-end build
#
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS api-build
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
FROM node:14.15.4-alpine AS client-build
WORKDIR /app

ENV PATH /app/node_modules/.bin:$PATH

COPY downloader-client/package.json downloader-client/package-lock.json ./
RUN npm ci
COPY downloader-client/. ./
RUN npm run build


#
# final stage/image
#
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine-amd64

RUN mkdir -p /data/completed /data/incomplete && \
    chmod 777 /data/completed /data/incomplete

WORKDIR /downloader-client
COPY --from=client-build /app/build ./
    
WORKDIR /downloader
COPY --from=api-build /app ./

VOLUME ["/data"]

ENTRYPOINT ["./Api",                                            \
            "--DownloadDirectories:Incomplete=/data/incomplete",\
            "--DownloadDirectories:Completed=/data/completed"]

EXPOSE 80
