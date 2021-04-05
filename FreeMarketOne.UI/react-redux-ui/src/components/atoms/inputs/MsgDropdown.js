import React, { useEffect, useRef } from "react";
import { useDispatch } from "react-redux";

// Actions
import { openConversation } from "Modules/units/Messages";

// Style
import "./Input.css";

const MsgDropdown = ({ open, handleOpen, messages }) => {
  const dispatch = useDispatch();
  const selectRef = useRef(null);

  useEffect(() => {
    const handleClickOutside = e => {
      const isOutsideClick =
        selectRef.current && !selectRef.current.contains(e.target);
      if (isOutsideClick && open) {
        handleOpen();
      }
    };
    document.addEventListener("click", handleClickOutside);

    return () => {
      document.removeEventListener("click", handleClickOutside);
    };
  }, [open]);

  const handleOpenConversation = (e, i) => {
    e.preventDefault();
    dispatch(openConversation(i));
  };

  return (
    <div className={open ? "dropdown open" : "dropdown"} ref={selectRef}>
      <span className="user-notifications" onClick={handleOpen}>
        <i className="ion-android-chat" />
      </span>{" "}
      <div className="dropdown-content">
        <div className="dropdown-menu-header">
          <span>Conversations</span>

          <i className="ion-clock" />
        </div>

        {messages?.data.length !== 0 ? (
          messages?.data?.map(i => {
            return (
              <span
                key={i.id}
                className="dropdown-message"
                onClick={e => handleOpenConversation(e, i)}
              >
                <div className="user-status online">
                  <i className="ion-android-radio-button-on" />
                  <span>{i.user}</span>
                  <span className="time-tag">{i.lastOnline}</span>
                </div>
              </span>
            );
          })
        ) : (
          <div className="no-messages">
            <i className="ion-android-chat fa" />
            <p>No messages...</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default MsgDropdown;
