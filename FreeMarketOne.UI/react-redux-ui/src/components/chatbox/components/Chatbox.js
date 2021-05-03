/* eslint-disable jsx-a11y/no-static-element-interactions */
/* eslint-disable jsx-a11y/click-events-have-key-events */
/* eslint-disable no-nested-ternary */
import React, { useState, useEffect, useRef } from "react";
import { useDispatch } from "react-redux";

import moment from "moment";

// Actions
import { closeConversation, sendMessage } from "Modules/units/Messages";

// Style
import "../Chatbox.css";

const Chatbox = ({ data }) => {
  const dispatch = useDispatch();

  const chatRef = useRef();

  // State
  const [open, setOpen] = useState(true);
  const [resizeInput, setResizeInput] = useState(false);
  const [resizeBox, setResizeBox] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    chatRef.current.scrollTo(0, chatRef.current.scrollHeight, "auto");
  }, []);

  const handleCloseConversation = (e, id) => {
    e.preventDefault();
    dispatch(closeConversation(id));
  };

  const handleMinimizeBox = () => {
    setOpen(!open);
  };

  const handleResizeInput = () => {
    setResizeInput(!resizeInput);
  };
  const handleResizeBox = () => {
    setResizeBox(!resizeBox);
  };
  const handleClearInput = () => {
    setMessage("");
  };

  const handleSendMessage = e => {
    e.preventDefault();
    const body = {
      id: Math.random(),
      text: message,
      date: moment().format(),
      messageSource: true
    };
    dispatch(sendMessage(data.id, body, handleClearInput));
  };

  return (
    <div
      className={
        open && !resizeBox
          ? "chatbox"
          : !open
          ? "chatbox-min"
          : open && resizeBox
          ? "chatbox big"
          : "chatbox"
      }
    >
      <div className="chatbox-top">
        <div className="chat-partner-name online">
          <i className="ion-android-radio-button-on" />
          <span> {data?.user}</span>
        </div>
        <div className="chatbox-icons">
          <span onClick={handleResizeBox}>
            <i className="ion-arrow-resize fa" />
          </span>
          <span onClick={handleMinimizeBox}>
            <i
              className={open ? "ion-ios-arrow-down fa" : "ion-ios-arrow-up fa"}
            />
          </span>
          <span onClick={e => handleCloseConversation(e, data?.id)}>
            <i className="ion-android-close fa" />
          </span>
        </div>
      </div>
      <div className="chat-messages" ref={chatRef}>
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
          value={message}
          onChange={e => setMessage(e.target.value)}
          className={!resizeInput ? "chat-input" : "chat-input-resize"}
          placeholder="Write a message..."
        />
        <span onClick={e => handleSendMessage(e)} className="send-btn">
          <i className="ion-android-send fas" />
        </span>
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
