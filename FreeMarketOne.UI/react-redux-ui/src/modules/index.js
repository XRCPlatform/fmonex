// Redux
import { combineReducers } from "redux";

// Units
import auth from "Modules/units/Auth";
import messages from "Modules/units/Messages";

export default combineReducers({
  auth,
  messages
});
