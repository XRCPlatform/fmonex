/**
|--------------------------------------------------
| IMPORTS
|--------------------------------------------------
*/
import messages from "../../api/messages.json";

sessionStorage.setItem("messages", JSON.stringify(messages));
/**
  |--------------------------------------------------
  | TYPES
  |--------------------------------------------------
  */
const GET_ALL_MSGS_REQ = "auth/GET_ALL_MSGS_REQ";
const GET_ALL_MSGS_SCS = "auth/GET_ALL_MSGS_SCS";
const GET_ALL_MSGS_FLR = "auth/GET_ALL_MSGS_FLR";

const OPEN_CONVERSATION = "OPEN_CONVERSATION";

const REMOVE_CONVERSATION = "REMOVE_CONVERSATION";

/**
 * ACTIONS
 */

export const getAllMessages = () => async dispatch => {
  dispatch({ type: GET_ALL_MSGS_REQ });
  const response = await JSON.parse(sessionStorage.getItem("messages"));

  if (response.status.errorCode === 200) {
    dispatch({
      type: GET_ALL_MSGS_SCS,
      payload: response.data
    });
  } else {
    dispatch({ type: GET_ALL_MSGS_FLR });
  }
};

export const openConversation = data => async dispatch => {
  dispatch({
    type: OPEN_CONVERSATION,
    payload: data
  });
};

export const removeConversation = id => async dispatch => {
  dispatch({
    type: REMOVE_CONVERSATION,
    payload: id
  });
};

/**
 * REDUCERS
 */
const INIT_STATE = {
  loading: false,
  data: [],
  conversations: []
};

export default function reducer(state = INIT_STATE, action = {}) {
  switch (action.type) {
    case GET_ALL_MSGS_REQ:
      return {
        ...state,
        loading: true,
        data: state.data
      };
    case GET_ALL_MSGS_SCS:
      return {
        ...state,
        loading: false,
        data: action.payload
      };
    case GET_ALL_MSGS_FLR:
      return {
        ...state,
        loading: false,
        data: state.data
      };

    case OPEN_CONVERSATION:
      return {
        ...state,
        loading: false,
        conversations: [...state.conversations, action.payload]
      };

    case REMOVE_CONVERSATION:
      return {
        ...state,
        conversations: state.conversations.filter(
          item => item.id !== action.payload
        )
      };

    default:
      return state;
  }
}
