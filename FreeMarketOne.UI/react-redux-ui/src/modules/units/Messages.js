/* eslint-disable no-case-declarations */
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

const CLOSE_CONVERSATION = "CLOSE_CONVERSATION";

const SEND_MESSAGE = "SEND_MESSAGE";

const REMOVE_EXCESS = "REMOVE_EXCESS";

/**
 * ACTIONS
 */

const isDuplicate = (data, obj) =>
  data.some(el =>
    Object.entries(obj).every(([key, value]) => value === el[key])
  );

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

export const closeConversation = id => async dispatch => {
  dispatch({
    type: CLOSE_CONVERSATION,
    payload: id
  });
};

export const sendMessage = (id, body, handleClearInput) => async dispatch => {
  dispatch({
    type: SEND_MESSAGE,
    id,
    data: body
  });
  handleClearInput();
};

export const removeExcess = () => async dispatch => {
  dispatch({
    type: REMOVE_EXCESS
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

        conversations: !isDuplicate(state.conversations, action.payload)
          ? [...state.conversations, action.payload]
          : state.conversations
      };

    case CLOSE_CONVERSATION:
      return {
        ...state,
        conversations: state.conversations.filter(
          item => item.id !== action.payload
        )
      };

    case SEND_MESSAGE:
      return {
        ...state,
        loading: false,
        conversations: state.conversations.map(item => {
          if (item.id === action.id) {
            item.messages.push(action.data);
            return item;
          }
          return item;
        })
      };

    case REMOVE_EXCESS:
      const arr = state.conversations;
      arr.shift();
      return {
        ...state,
        conversations: arr
      };

    default:
      return state;
  }
}
