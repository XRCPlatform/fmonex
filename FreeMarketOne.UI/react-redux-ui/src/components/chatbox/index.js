import React, { useState, useEffect } from "react";
import { useSelector, useDispatch } from "react-redux";

// Actions
import { removeExcess } from "Modules/units/Messages";

// Components
import Chatbox from "./components/Chatbox";
// Style
import "./Chatbox.css";

const Chat = () => {
  const dispatch = useDispatch();

  // Redux
  const conversations = useSelector(state => state.messages?.conversations);

  // State
  const [conv, setConv] = useState([]);

  useEffect(() => {
    setConv(conversations);
    if (conversations.length > 3) {
      dispatch(removeExcess());
    } else {
      setConv(conversations);
    }
  }, [conversations]);

  return (
    <div className="chatbox-holder">
      {conv?.length > 0 ? (
        conv?.map(item => {
          return <Chatbox key={item?.id} data={item} id={item?.id} />;
        })
      ) : (
        <div />
      )}
    </div>
  );
};

export default Chat;
