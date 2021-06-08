/**
|--------------------------------------------------
| IMPORTS
|--------------------------------------------------
*/

/**
|--------------------------------------------------
| TYPES
|--------------------------------------------------
*/

const LOGIN_SCS = "auth/LOGIN_SCS";

const LOGOUT_USER = "auth/LOGOUT_USER";

/**
|--------------------------------------------------
| ACTIONS
|--------------------------------------------------
*/

export const login = (history, setLoad) => async dispatch => {
  const response = localStorage.setItem("user", true);
  dispatch({ type: LOGIN_SCS, payload: { user: response } });
  setLoad(true);
  /*  history.push("/"); */
};

export const logOut = history => dispatch => {
  dispatch({ type: LOGOUT_USER });
  localStorage.setItem("user", false);
  history.push("/login");
};

/**
|--------------------------------------------------
| REDUCERS
|--------------------------------------------------
*/

const INIT_STATE = {
  user: JSON.parse(localStorage.getItem("user")),
  loading: false
};

export default function reducer(state = INIT_STATE, action = {}) {
  switch (action.type) {
    case LOGIN_SCS:
      return {
        ...state,
        user: action.payload.user,
        loading: false
      };

    default:
      return state;
  }
}
