import { useState, MouseEvent } from "react";
import { postDataAsJson } from "../utils";
import InputText from "./InputText";

import "./PostForm.css";

interface Props {
  onDownloadAdded: (id: string) => Promise<void>;
}

export default function PostForm(props: Props) {
  const [link, setLink] = useState<string>("");
  const [saveAs, setSaveAs] = useState<{ value: string; userChanged: boolean }>(
    { value: "", userChanged: false }
  );

  const handleLinkInputChange = (
    link: string,
    saveAsUserChanged: boolean
  ): void => {
    setLink(link);
    try {
      const linkUrl = new URL(link);
      const fileName = linkUrl.pathname.split("/").pop();
      if (fileName && !saveAsUserChanged) {
        setSaveAs({
          value: fileName,
          userChanged: saveAsUserChanged,
        });
      }
    } catch (error) {
      if (error instanceof TypeError !== true) {
        throw error;
      }
    }
  };

  const handleSaveAsInputChange = (value: string): void => {
    const userChanged = value !== "";
    setSaveAs({ value: value, userChanged: userChanged });
  };

  const handleSubmit = async (
    event: MouseEvent<HTMLButtonElement>,
    link: string,
    saveAsValue: string
  ): Promise<void> => {
    event.preventDefault();
    event.persist();
    if (!link) {
      return;
    }
    const dto = {
      Link: link,
      SaveAsFileName: saveAsValue,
    };
    try {
      const responseData = await postDataAsJson("/api/download", dto);
      await props.onDownloadAdded(responseData);
      resetForm();
    } catch (error) {
      console.log("Error detected: " + error);
    }
  };

  const resetForm = () => {
    setLink("");
    setSaveAs({ value: "", userChanged: false });
  };

  return (
    <form className="PostForm">
      <span className="PostForm-Link">
        <InputText
          label="Link:"
          data-testid="link-input"
          value={link}
          onChange={(link) => handleLinkInputChange(link, saveAs.userChanged)}
        />
      </span>
      <InputText
        label="Save As:"
        data-testid="save-as-input"
        value={saveAs.value}
        onChange={(saveAsValue) => handleSaveAsInputChange(saveAsValue)}
      />
      <button
        disabled={!link || !saveAs.value}
        onClick={(e) => handleSubmit(e, link, saveAs.value)}
      >
        Submit
      </button>
    </form>
  );
}
