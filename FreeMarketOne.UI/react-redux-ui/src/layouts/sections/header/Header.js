// React, Redux, Router
import React, { useState, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";

// Actions
import { getAllMessages } from "Modules/units/Messages";

// Atoms
import DividerVerticalSmall from "Components/atoms/UI/DividerVerticalSmall";
import Select from "Components/atoms/inputs/Select";
import MsgDropdown from "Components/atoms/inputs/MsgDropdown";
import UserDropdown from "Components/atoms/inputs/UserDropdown";

import "./Header.css";

const OPTIONS = [
  { id: "765", name: "XRC" },
  { id: "432", name: "BTC" },
  { id: "645", name: "LTC" }
];

const Header = ({ handleOpenSearch }) => {
  const dispatch = useDispatch();

  // Redux
  const messages = useSelector(state => state.messages);

  // State
  const [selected, setSelected] = useState(OPTIONS[0]);
  const [openMsg, setOpenMsg] = useState(false);
  const [openUser, setOpenUser] = useState(false);

  useEffect(() => {
    dispatch(getAllMessages());
  }, []);

  const handleOpenkMsg = () => {
    setOpenMsg(!openMsg);
  };
  const handleOpenUser = () => {
    setOpenUser(!openUser);
  };

  return (
    <>
      <div className="header">
        <div className="logo">
          free<span>market</span>one
        </div>
        <div className="header_ctrl">
          <span className="search-btn">
            <i className="ion-ios-search" onClick={handleOpenSearch} />
          </span>
          <DividerVerticalSmall />
          <MsgDropdown
            open={openMsg}
            handleOpen={handleOpenkMsg}
            messages={messages}
          />

          <DividerVerticalSmall />

          <UserDropdown open={openUser} handleOpen={handleOpenUser} />

          <DividerVerticalSmall />
          <Select options={OPTIONS} value={selected} onChange={setSelected} />
        </div>
      </div>
    </>
  );
};

export default Header;
