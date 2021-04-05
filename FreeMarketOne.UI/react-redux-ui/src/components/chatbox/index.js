import React, { useState, useEffect } from "react";
import { useSelector } from "react-redux";

import moment from "moment";

// Components
import Chatbox from "./components/Chatbox";
// Style
import "./Chatbox.css";

const Chat = () => {
  // Redux
  const conversations = useSelector(state => state.messages?.conversations);

  // State
  const [conv, setConv] = useState([]);

  useEffect(() => {
    setConv(conversations);
  }, [conversations]);

  return (
    <div className="chatbox-holder">
      {conv?.length > 0 ? (
        conv?.map(item => {
          return <Chatbox key={item?.id} data={item} />;
        })
      ) : (
        <div />
      )}
    </div>
  );
};

export default Chat;
