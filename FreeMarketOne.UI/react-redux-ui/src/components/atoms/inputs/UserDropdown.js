import React, { useEffect, useRef } from "react";

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
      <span className="user-login" onClick={handleOpen}>
        <i className="ion-ios-contact-outline"> </i>
      </span>{" "}
    </div>
  );
};

export default UserDropdown;
