import { Download } from "../dto/Download";
import { DownloadStatus } from "../dto/DownloadStatus";

import "./DownloadList.css";

interface Props {
  downloadDtos: Download[];
}

export default function DownloadList(props: Props) {
  /* Descending sort by createdTicks value */
  const sortedDtos = props.downloadDtos.sort(
    (a, b) => b.createdTicks - a.createdTicks
  );

  return (
    <table className="DownloadList">
      <tbody>
        {sortedDtos.map((dto) => (
          <DownloadItem key={dto.id} dto={dto} />
        ))}
      </tbody>
    </table>
  );
}

function DownloadItem(props: { dto: Download }) {
  const {
    saveAsFile,
    link,
    status,
    bytesDownloaded,
    totalBytes,
    reasonForFailure,
  } = props.dto;
  const totalBytesString: string =
    totalBytes < 0 ? "?" : totalBytes.toLocaleString();

  const getStatusClass = (status: DownloadStatus): string => {
    switch (status) {
      case DownloadStatus.NotStarted:
        return "DownloadItem-NotStarted";
      case DownloadStatus.Downloading:
        return "DownloadItem-Downloading";
      case DownloadStatus.Completed:
        return "DownloadItem-Completed";
      case DownloadStatus.Failed:
        return "DownloadItem-Failed";
      default:
        return ""; // unknown
    }
  };

  return (
    <tr title={link}>
      <td className="DownloadItem-SaveAsFile">{saveAsFile}</td>
      <td className={getStatusClass(status)} title={reasonForFailure}>
        {status}
      </td>
      <td className="DownloadItem-Bytes">{bytesDownloaded.toLocaleString()}</td>
      <td>/</td>
      <td className="DownloadItem-Bytes">{totalBytesString}</td>
    </tr>
  );
}
