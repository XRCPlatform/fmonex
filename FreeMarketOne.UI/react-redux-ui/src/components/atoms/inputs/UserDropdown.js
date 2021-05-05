import React, { useEffect, useRef } from "react";

import Button from "../buttons/ButtonWithEffect";
import Rating from "../UI/Rating";

import "./Input.css";

const UserDropdown = ({ open, handleOpen }) => {
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

  return (
    <div className={open ? "dropdown open" : "dropdown"} ref={selectRef}>
      <span className="user-login icon" onClick={handleOpen}>
        <i className="ion-ios-contact-outline"> </i>
        {!open && <div className="tooltipp tooltip-bott">Account</div>}
      </span>{" "}
      <div className="dropdown-content usr">
        <div className="dropdown-menu-header">
          <Rating />
        </div>
        <div className="section">
          <span>
            <i className="ion-android-options" />
            <span>Settings</span>
          </span>
        </div>
        <div className="section">
          <span>
            <i className="ion-bug" />
            Report bug
          </span>
        </div>
        <div className="section">
          <span>
            <i className="ion-easel" />
            Suggest feature
          </span>
        </div>
        <div className="section">
          <span>
            <i className="ion-log-out" />
            Logout
          </span>
        </div>
        <Button title="Login / Signup" />
      </div>
    </div>
  );
};

export default UserDropdown;
