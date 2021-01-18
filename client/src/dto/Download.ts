import { DownloadStatus } from "./DownloadStatus";

export interface Download {
  id: string;
  link: string;
  saveAsFile: string;
  status: DownloadStatus;
  createdTicks: number;
  totalBytes: number;
  bytesDownloaded: number;
  reasonForFailure: string;
}
