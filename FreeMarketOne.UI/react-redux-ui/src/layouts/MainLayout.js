// React
import React, { useState } from "react";

// Layout components
import Header from "Layouts/sections/header/Header";
import Footer from "Layouts/sections/footer/Footer";
import Sidebar from "Layouts/sections/sidebar/Sidebar";

const MainLayout = state => {
  const [show, setShow] = useState(false);

  const handleToggle = () => {
    setShow(!show);
  };

  return (
    <>
      <Header />
      <div id="wrapper" className={show ? "toggled" : ""}>
        <Sidebar show={show} />
        <div id="page-content-wrapper">
          <div>{state.children}</div>
        </div>
      </div>
      <Footer handleToggle={handleToggle} show={show} />
    </>
  );
};

export default MainLayout;
