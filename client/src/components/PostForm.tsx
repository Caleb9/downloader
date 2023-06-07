import { useState, MouseEvent } from "react";
import { postDataAsJson } from "../utils";
import InputText from "./InputText";

import "./PostForm.css";

interface Props {
  onDownloadAdded: (id: string) => Promise<void>;
}

export default function PostForm(props: Props) {
  const [link, setLink] = useState<string>("");
  const [saveAs, setSaveAs] = useState<{
    value: string;
    userChanged: boolean;
    isValid: boolean;
  }>({ value: "", userChanged: false, isValid: false });

  const handleLinkInputChange = (
    link: string,
    saveAsUserChanged: boolean
  ): void => {
    setLink(link);
    try {
      if (saveAsUserChanged) {
        return;
      }
      const linkUrl = new URL(link);
      const fileName = linkUrl.pathname.split("/").pop() ?? "";
      setSaveAs({
        value: fileName,
        userChanged: false,
        isValid: isSaveAsValid(fileName),
      });
    } catch (error) {
      if (!saveAsUserChanged) {
        setSaveAs({
          value: "",
          userChanged: false,
          isValid: false,
        });
      }
      if (error instanceof TypeError !== true) {
        throw error;
      }
    }
  };

  const handleSaveAsInputChange = (value: string): void => {
    const userChanged = value !== "";
    setSaveAs({
      value: value,
      userChanged: userChanged,
      isValid: isSaveAsValid(value),
    });
  };

  /** Naive path validation allowing for sub-directories */
  const isSaveAsValid = (value: string | undefined): boolean => {
    if (value === undefined) {
      return false;
    }
    value = value.trim();
    return (
      value !== "" &&
      value !== "." &&
      !value.startsWith("/") &&
      !value.endsWith("/") &&
      !value.endsWith("/.") &&
      !value.includes("..")
    );
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
      resetForm();
      const responseData = await postDataAsJson("/api/download", dto);
      await props.onDownloadAdded(responseData);
    } catch (error) {
      console.log("Error detected: " + error);
    }
  };

  const resetForm = () => {
    setLink("");
    setSaveAs({ value: "", userChanged: false, isValid: false });
  };

  return (
    <form className="PostForm">
      <span className="PostForm-Link">
        <InputText
          label="Link:"
          title="HTTP link"
          data-testid="link-input"
          value={link}
          onChange={(link) => handleLinkInputChange(link, saveAs.userChanged)}
          isValid={true}
        />
      </span>
      <InputText
        label="Save As:"
        title={
          "Customize file name, optionally specifying a relative folder path " +
          "(folders will be created)"
        }
        data-testid="save-as-input"
        value={saveAs.value}
        onChange={(saveAsValue) => handleSaveAsInputChange(saveAsValue)}
        isValid={saveAs.value === "" || saveAs.isValid}
      />
      <button
        disabled={!link || !saveAs.isValid}
        onClick={(e) => handleSubmit(e, link, saveAs.value)}
      >
        Submit
      </button>
    </form>
  );
}
