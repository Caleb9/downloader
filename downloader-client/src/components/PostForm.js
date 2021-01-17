import React from "react";
import { postDataAsJson } from "../utils";

class PostForm extends React.Component {
  constructor(props) {
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

  handleSubmit(event) {
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

  handleLinkInputChange(value) {
    this.setState({ link: value });
    try {
      const linkUrl = new URL(value);
      const fileName = linkUrl.pathname.split("/").pop();
      if (fileName && !this.state.saveAs.userChanged) {
        this.setState({ saveAs: { value: fileName } });
      }
    } catch (error) {
      if (error instanceof TypeError !== true) {
        throw error;
      }
    }
  }

  handleSaveAsInputChange(value) {
    const userChanged = value && value !== "";
    this.setState({ saveAs: { value: value, userChanged: userChanged } });
  }

  render() {
    return (
      <>
        <InputText
          label="Link:"
          value={this.state.link}
          onChange={this.handleLinkInputChange}
        />
        <label>{this.state.defaultSaveAsFileName}</label>
        <InputText
          label="Save As:"
          value={this.state.saveAs.value}
          onChange={this.handleSaveAsInputChange}
        />
        <button disabled={!this.state.link} onClick={this.handleSubmit}>
          Submit
        </button>
        {/* <input type="submit" value="Submit" /> */}
      </>
    );
  }
}

function InputText(props) {
  return (
    <>
      <label>{props.label}</label>
      <input
        type="text"
        value={props.value}
        onChange={(event) => props.onChange(event.target.value)}
      />
    </>
  );
}

export default PostForm;
