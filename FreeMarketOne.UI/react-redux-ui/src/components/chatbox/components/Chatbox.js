import React, { useState } from "react";
import { useDispatch } from "react-redux";

import moment from "moment";

// Actions
import { removeConversation } from "Modules/units/Messages";

// Style
import "../Chatbox.css";

const Chatbox = ({ data }) => {
  const dispatch = useDispatch();

  // State
  const [open, setOpen] = useState(true);
  const [resizeInput, setResizeInput] = useState(false);

  const handleRemove = (e, id) => {
    e.preventDefault();
    dispatch(removeConversation(id));
  };

  const handleMinimize = () => {
    setOpen(!open);
  };

  const handleResizeInput = () => {
    setResizeInput(!resizeInput);
  };

  return (
    <div className={open ? "chatbox" : "chatbox-min"}>
      <div className="chatbox-top">
        <div className="chat-partner-name online">
          <i className="ion-android-radio-button-on" />
          <span> {data?.user}</span>
        </div>
        <div className="chatbox-icons">
          <span>
            <i className="ion-arrow-resize fa" />
          </span>
          <span onClick={handleMinimize}>
            <i
              className={open ? "ion-ios-arrow-down fa" : "ion-ios-arrow-up fa"}
            />
          </span>
          <span onClick={e => handleRemove(e, data?.id)}>
            <i className="ion-android-close fa" />
          </span>
        </div>
      </div>
      <div div className="chat-messages">
        {data?.messages?.map(i => {
          return i?.messageSource === true ? (
            <div key={i?.id} className="message-box-holder">
              <div className="message-box">
                {i?.text}
                <span className="time-tag-bubble">
                  {moment(i?.date).format("MM.DD.YYYY, HH:mm")}
                </span>
              </div>
            </div>
          ) : (
            <div key={i?.id} className="message-box-holder">
              <div className="message-box message-partner">
                {i?.text}
                <span className="time-tag-bubble">
                  {moment(i?.date).format("MM.DD.YYYY, HH:mm")}
                </span>
              </div>
            </div>
          );
        })}
      </div>
      <div className="chat-input-holder">
        <textarea
          className={!resizeInput ? "chat-input" : "chat-input-resize"}
          placeholder="Write a message..."
        />
        <i className="ion-android-send fas" />
        <span onClick={handleResizeInput}>
          <i
            className={
              resizeInput ? "ion-ios-arrow-down res" : "ion-ios-arrow-up res"
            }
          />
        </span>
      </div>
    </div>
  );
};

export default Chatbox;
