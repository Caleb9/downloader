import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "./App";

describe("PostForm",  () => {
  test("saveAs value is set to file name when link is an HTTP URL", async () => {
    render(<App />);
    const linkInput = screen.getByTestId<HTMLInputElement>("link-input");
    const saveAsInput: any = screen.getByTestId<HTMLInputElement>("save-as-input");
    linkInput.focus();

    await userEvent.paste("http://download.me/fileName.iso");

    expect(saveAsInput.value).toBe("fileName.iso");
  });
});
