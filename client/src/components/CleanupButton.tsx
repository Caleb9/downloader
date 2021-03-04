import { MouseEvent } from "react";
import { sendDelete } from "../utils";
import "./CleanupButton.css";

interface Props {
  enabled: boolean;
  onClick: () => void;
}

export default function CleanupButton(props: Props) {
  const handleClick = async (event: MouseEvent) => {
    event.preventDefault();
    event.persist();
    await sendDelete("/api/download");
    props.onClick();
  };

  return (
    <button
      className="CleanupButton"
      onClick={handleClick}
      disabled={!props.enabled}
    >
      Cleanup
    </button>
  );
}
