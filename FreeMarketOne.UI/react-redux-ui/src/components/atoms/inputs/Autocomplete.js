// React, Redux, Router
import React from "react";

import "./Input.css";

const Autocomplete = props => {
  const { service, open } = props;

  const sendValue = e => {
    console.log(e.target.value);
  };

  const handleSubmit = () => {
    console.log("Submit");
  };

  const handleKeyDown = e => {
    if (e.key === "Enter") {
      console.log("Enter");
    }
  };

  return (
    <>
      <div className={open ? "search-content active" : "search-content"}>
        <div className="search">
          <div className="search-box">
            <input
              type="text"
              onChange={sendValue}
              onKeyDown={handleKeyDown}
              placeholder="Search in offers (leave empty to search for all offers)..."
            />
            <button type="button" onClick={handleSubmit}>
              <i className="ion-ios-search-strong" />
            </button>
          </div>
        </div>
      </div>
    </>
  );
};

export default Autocomplete;
