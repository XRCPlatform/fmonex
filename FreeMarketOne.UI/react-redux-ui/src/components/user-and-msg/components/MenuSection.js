import React from "react";

import Rating from "../../atoms/UI/Rating";

import "../UsrMsg.css";

const MenuSection = ({ setLoginTest }) => {
  return (
    <>
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
        <span onClick={setLoginTest}>
          <i className="ion-log-out" />
          Logout
        </span>
      </div>
    </>
  );
};

export default MenuSection;
