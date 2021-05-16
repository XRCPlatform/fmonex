import React, { useState, useEffect, useRef } from "react";

import MenuSection from "./components/MenuSection";
import LoginSignupSection from "./components/LoginSignupSection";

import "./UsrMsg.css";

const UserDropdown = ({ open, handleOpen }) => {
  const selectRef = useRef(null);

  const [loginTest, setLoginTest] = useState(false);

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

  const handleSetLoginTest = () => {
    setLoginTest(!loginTest);
  };

  return (
    <div className={open ? "dropdown open" : "dropdown"} ref={selectRef}>
      <span className="user-login icon" onClick={handleOpen}>
        <i className="ion-ios-contact-outline"> </i>
        {!open && <div className="tooltipp tooltip-bott">Account</div>}
      </span>{" "}
      <div className="dropdown-content usr">
        {loginTest ? (
          <MenuSection setLoginTest={handleSetLoginTest} />
        ) : (
          <LoginSignupSection setLoginTest={handleSetLoginTest} />
        )}
      </div>
    </div>
  );
};

export default UserDropdown;
