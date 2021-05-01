// React
import React, { useState } from "react";

// Layout components
import Header from "Layouts/sections/header/Header";
import Footer from "Layouts/sections/footer/Footer";
import Sidebar from "Layouts/sections/sidebar/Sidebar";
import Autocomplete from "Components/atoms/inputs/Autocomplete";
import Chatbox from "Components/chatbox";

const PrivateLayout = state => {
  const [show, setShow] = useState(false);
  const [activeSearch, setActiveSearc] = useState(false);

  const handleToggle = () => {
    setShow(!show);
  };
  const handleOpenSearch = () => {
    setActiveSearc(!activeSearch);
  };
  return (
    <>
      <Header handleOpenSearch={handleOpenSearch} activeSearch={activeSearch} />
      <Autocomplete open={activeSearch} />
      <div id="wrapper" className={show ? "toggled" : ""}>
        <Sidebar show={show} />
        <div id="page-content-wrapper">
          <div>{state.children}</div>
          <Chatbox />
        </div>
      </div>

      <Footer handleToggle={handleToggle} show={show} />
    </>
  );
};

export default PrivateLayout;
