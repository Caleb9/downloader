import "./App.css";
import { useState, useEffect } from "react";
import * as signalR from "@microsoft/signalr";

import DownloadList from "./components/DownloadList";
import PostForm from "./components/PostForm";
import { Download } from "./dto/Download";
import {
  TotalBytesMessage,
  ProgressMessage,
  FinishedMessage,
  FailedMessage,
} from "./dto/Messages";
import { DownloadStatus } from "./dto/DownloadStatus";

interface DownloadsDictionary {
  [id: string]: Download;
}

export default function App() {
  const [downloads, setDownloads] = useState<DownloadsDictionary>({});
  const [hubConnection] = useState<signalR.HubConnection>(
    new signalR.HubConnectionBuilder()
      .withUrl("/hub")
      .configureLogging(signalR.LogLevel.Warning)
      .build()
  );

  useEffect(() => {
    const fetchDownloads = async () => {
      const response: Response = await fetch("/api/download");
      const downloadsArray: Download[] = await response.json();
      setDownloads((prevDownloads) => {
        const newDownloads: DownloadsDictionary = { ...prevDownloads };
        downloadsArray.forEach((d) => (newDownloads[d.id] = d));
        return newDownloads;
      });
    };
    fetchDownloads();
  }, []);

  useEffect(() => {
    if (hubConnection.state !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    const setNewDownloads = (
      downloadsUpdateLogic: (downloads: DownloadsDictionary) => void
    ) =>
      setDownloads((prevDownloads) => {
        const newDownloads: DownloadsDictionary = { ...prevDownloads };
        downloadsUpdateLogic(newDownloads);
        return newDownloads;
      });

    hubConnection.on("receiveTotalBytes", (message: TotalBytesMessage) =>
      setNewDownloads((downloads: DownloadsDictionary) => {
        const { id, totalBytes } = message;
        if (id in downloads) {
          downloads[id].totalBytes = totalBytes;
        }
      })
    );
    hubConnection.on("receiveProgress", (message: ProgressMessage[]) =>
      setNewDownloads((downloads) =>
        message
          .filter(
            (p) =>
              p.id in downloads &&
              downloads[p.id].bytesDownloaded < p.bytesDownloaded
          )
          .forEach((p) => (downloads[p.id].bytesDownloaded = p.bytesDownloaded))
      )
    );
    hubConnection.on("receiveFinished", (message: FinishedMessage) =>
      setNewDownloads((downloads) => {
        const { id, fileName } = message;
        if (id in downloads) {
          const download = downloads[id];
          download.status = DownloadStatus.Completed;
          download.saveAsFile = fileName;
        }
      })
    );
    hubConnection.on("receiveFailed", (message: FailedMessage) =>
      setNewDownloads((downloads) => {
        const { id, reason } = message;
        if (id in downloads) {
          const download = downloads[id];
          download.status = DownloadStatus.Failed;
          download.reasonForFailure = reason;
        }
      })
    );
    hubConnection.start().catch((error) => console.log(error));
  }, []);

  const handleDownloadAdded = async (id: string) => {
    const response: Response = await fetch(`/api/download/${id}`);
    const data: Download = await response.json();
    setDownloads((prevDownloads) => {
      const newDownloads = { ...prevDownloads };
      newDownloads[id] = data;
      return newDownloads;
    });
  };

  return (
    <main className="App">
      <PostForm onDownloadAdded={handleDownloadAdded} />
      <DownloadList downloadDtos={Object.values(downloads)} />
    </main>
  );
}
