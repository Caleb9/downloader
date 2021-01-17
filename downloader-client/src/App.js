import "./App.css";
import React from "react";
import PostForm from "./components/PostForm";

class App extends React.Component {
  constructor(props) {
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

  render() {
    return (
      <div className="App">
        <header className="App-header">
          <PostForm />
          <h2>Downloads</h2>
          <ol>
            {this.state.values.map((value) => (
              <li key={value.id}>
                {value.id} {value.status}
              </li>
            ))}
          </ol>
        </header>
      </div>
    );
  }
}

export default App;
