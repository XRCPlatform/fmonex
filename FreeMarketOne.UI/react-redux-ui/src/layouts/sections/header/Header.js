// React, Redux, Router
import React, { useState } from "react";

// Atoms
import DividerVerticalSmall from "Components/atoms/UI/DividerVerticalSmall";
import Autocomplete from "Components/atoms/inputs/Autocomplete";
import Select from "Components/atoms/inputs/Select";

import "./Header.css";

const OPTIONS = [
  { id: "223", name: "USD" },
  { id: "432", name: "BTC" },
  { id: "645", name: "EUR" },
  { id: "234", name: "USDT" },
  { id: "765", name: "XRC" },
  { id: "123", name: "ETH" },
  { id: "er5", name: "XRP" },
  { id: "356", name: "DOGE" }
];

const Header = () => {
  const [selected, setSelected] = useState(OPTIONS[0]);

  return (
    <div className="header">
      <div className="logo">
        free<span>market</span>one
      </div>

      <div>
        <Autocomplete />
      </div>
      <div className="header_ctrl">
        <span className="user-notifications">
          <i className="ion-android-chat"> </i>
        </span>

        <DividerVerticalSmall />

        <span className="user-login">
          <i className="ion-ios-contact-outline"> </i>
        </span>
        <DividerVerticalSmall />
        <Select options={OPTIONS} value={selected} onChange={setSelected} />
      </div>
    </div>
  );
};

export default Header;
