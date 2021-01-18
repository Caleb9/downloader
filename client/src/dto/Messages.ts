interface TotalBytesMessage {
  id: string;
  totalBytes: number;
}

interface ProgressMessage {
  id: string;
  bytesDownloaded: number;
}

interface FinishedMessage {
  id: string;
  fileName: string;
}

interface FailedMessage {
  id: string;
  reason: string;
}

export type {
  TotalBytesMessage,
  ProgressMessage,
  FinishedMessage,
  FailedMessage,
};
