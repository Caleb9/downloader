import "./App.css";
import React from "react";
import PostForm from "./components/PostForm";

export default class App extends React.Component<{}, IAppState> {
  constructor(props: any) {
    super(props);
    this.state = { values: [] };
  }

  componentDidMount() {
    fetch("/api/download")
      .then((response) => response.json())
      .then((data) => {
        this.setState({ values: data });
      });
  }

  render = () => (
    <div className="App">
      <header className="App-header">
        <PostForm />
        <h2>Downloads</h2>
        <ol>
          {this.state.values.map((value: IGetAllDto) => (
            <li key={value.id}>
              {value.id} {value.status}
            </li>
          ))}
        </ol>
      </header>
    </div>
  );
}

interface IAppState {
  values: IGetAllDto[];
}

interface IGetAllDto {
  id: string;
  status: string;
}
