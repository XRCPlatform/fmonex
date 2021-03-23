// React
import React from "react";

// Router
import { Router, Route, Switch } from "react-router-dom";
import MainRouter from "Router/MainRouter";
import { createBrowserHistory } from "history";

// HMR
import { hot } from "react-hot-loader";

// Redux
import { Provider } from "react-redux";
import configureStore from "Modules/configureStore";
import "./styles/App.css";

require("babel-polyfill");

const history = createBrowserHistory();

const App = () => {
  return (
    <Provider store={configureStore()}>
      <Router history={history}>
        <Switch>
          <Route path="/" component={MainRouter} />
        </Switch>
      </Router>
    </Provider>
  );
};

export default hot(module)(App);
