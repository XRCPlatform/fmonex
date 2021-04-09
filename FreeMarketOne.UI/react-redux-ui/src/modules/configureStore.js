// Redux
import { createStore, applyMiddleware } from "redux";
import { composeWithDevTools } from "redux-devtools-extension";

// Middlewares
import { createLogger } from "redux-logger";
import thunkMiddleware from "redux-thunk";

// Reducers
import rootReducer from "./index";

const configureStore = () => {
  const store = createStore(
    rootReducer,
    composeWithDevTools(
      applyMiddleware(thunkMiddleware, createLogger({ collapsed: true }))
    )
  );

  if (module.hot) {
    module.hot.accept("./index.js", () => {
      // eslint-disable-next-line global-require
      const nextRootReducer = require("./index.js");
      store.replaceReducer(nextRootReducer);
    });
  }

  return store;
};

export default configureStore;
