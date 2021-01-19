import React from "react";
import { postDataAsJson } from "../utils";
import InputText from "./InputText";

interface IPostFormState {
  link: string;
  saveAs: {
    value: string;
    userChanged: boolean;
  };
}

export default class PostForm extends React.Component<{}, IPostFormState> {
  constructor(props: any) {
    super(props);

    this.state = {
      link: "",
      saveAs: {
        value: "",
        userChanged: false,
      },
    };

    this.handleSubmit = this.handleSubmit.bind(this);
    this.handleLinkInputChange = this.handleLinkInputChange.bind(this);
    this.handleSaveAsInputChange = this.handleSaveAsInputChange.bind(this);
  }

  handleSubmit(event: React.MouseEvent<HTMLButtonElement, MouseEvent>) {
    const { link, saveAs } = this.state;
    if (!link) {
      return;
    }
    const dto = {
      Link: link,
      SaveAsFileName: saveAs.value,
    };
    postDataAsJson("/api/download", dto)
      .then((data) => console.log(data)) // TODO do something with the result
      .catch((error) => console.log("Error detected: " + error));
    event.preventDefault();
  }

  handleLinkInputChange(value: string) {
    this.setState({ link: value });
    try {
      const linkUrl = new URL(value);
      const fileName = linkUrl.pathname.split("/").pop();
      if (fileName && !this.state.saveAs.userChanged) {
        this.setState((prevState) => ({
          saveAs: {
            value: fileName,
            userChanged: prevState.saveAs.userChanged,
          },
        }));
      }
    } catch (error) {
      if (error instanceof TypeError !== true) {
        throw error;
      }
    }
  }

  handleSaveAsInputChange(value: string) {
    const userChanged = value !== "";
    this.setState({ saveAs: { value: value, userChanged: userChanged } });
  }

  render = () => (
    <>
      <InputText
        label="Link:"
        value={this.state.link}
        onChange={this.handleLinkInputChange}
      />
      <InputText
        label="Save As:"
        value={this.state.saveAs.value}
        onChange={this.handleSaveAsInputChange}
      />
      <button disabled={!this.state.link} onClick={this.handleSubmit}>
        Submit
      </button>
    </>
  );
}
