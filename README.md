# Downloader

[![Docker Image Version (latest) on Docker Hub](https://img.shields.io/docker/v/caleb9/downloader?label=Docker%20Hub&style=flat-square "Docker Image Version (latest) on Docker Hub")](https://hub.docker.com/r/caleb9/downloader)

Dockerized downloader.

I'm using it on my Synology NAS.

![](https://github.com/Caleb9/downloader/raw/master/doc/images/downloader.png)


## Disclaimer

I am only just starting to learn TypeScript and ReactJS. The code of
the Web client leaves a lot to be desired in terms of quality and
testability, I'm sure. It certainly could look better too. Things
might improve with time, I'm also open for co-op. Meanwhile, I beg for
your understanding ;).


## Building the Docker image

If `make` is available then simply
```
make
```
Otherwise
```
docker build --tag=caleb9/downloader .
```


## Running

```
docker run --publish 5000:80 --volume /tmp/downloads:/data --user 1000:1000 caleb9/downloader
```

* Replace `5000` with a port of your choice. The application will be
  available at `http://localhost:5000` (replace 5000 with the port you
  specified).
* Replace `/tmp/downloads` with a path to where files should be
  downloaded. Two sub-directories will be created: `incomplete` for
  temporary files, and `completed` for finished downloads.
* Replace `1000:1000` with _uid_ and _guid_ of a user that will run
  the container - downloaded files will have owner and group
  permissions set to these values (you can use your local user, find
  out the values using `id` command).


## Usage

In short:

1. Type or paste a HTTP(S) link to the "Link" field.
2. Optionally adjust the "Save As" field if you want to save under a
   different name. Application tries to guess the file name from
   provided Link URL, but it might not always succeed (e.g. when link
   redirects to another URL from which the actual file will be
   downloaded).
3. Hit "Submit" button to start the download. When completed, file
   will appear in 'completed' sub-directory.

"Cleanup" button clears completed and failed downloads (downloaded
files are not removed).

If downloaded file already exists, the fresh download will get a
number suffix in a similar way as downloading via browser (e.g. if you
already downloaded `file.iso` and try to download it again,
`file(1).iso` will be created).

## Things TODO

* Better styling of client
* Tests for client
* Pausing and cancelling a download
* Adding a download without immediately starting it
* Resuming a download (would require persistence of some sort)
* Downloading in several segments
* Support for other protocols than HTTP
