import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "./App";

describe("PostForm", () => {
  test("saveAs value is set to file name when link is an HTTP URL", () => {
    render(<App />);
    const linkInput = screen.getByTestId("link-input");
    const saveAsInput: any = screen.getByTestId("save-as-input");

    userEvent.paste(linkInput, "http://download.me/fileName.iso");

    expect(saveAsInput.value).toBe("fileName.iso");
  });
});
