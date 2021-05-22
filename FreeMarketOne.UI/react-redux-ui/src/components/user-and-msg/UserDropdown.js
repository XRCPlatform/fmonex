import React, { useEffect, useRef } from "react";
import { useDispatch } from "react-redux";
import { useHistory } from "react-router-dom";

// Actions
import { logOut } from "Modules/units/Auth";

import MenuSection from "./components/MenuSection";

import "./UsrMsg.css";

const UserDropdown = ({ open, handleOpen }) => {
  const dispatch = useDispatch();
  const history = useHistory();
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

  const handleSubmit = e => {
    e.preventDefault();

    dispatch(logOut(history));
  };

  return (
    <div className={open ? "dropdown open" : "dropdown"} ref={selectRef}>
      <span className="user-login icon" onClick={handleOpen}>
        <i className="ion-ios-contact-outline"> </i>
        {!open && <div className="tooltipp tooltip-bott">Account</div>}
      </span>{" "}
      <div className="dropdown-content usr">
        <MenuSection setLoginTest={handleSubmit} />
      </div>
    </div>
  );
};

export default UserDropdown;
