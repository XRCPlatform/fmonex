// React, Redux, Router
import React from "react";

// Atoms
import DividerVerticalSmall from "Components/atoms/UI/DividerVerticalSmall";

import "./Footer.css";

const Footer = ({ handleToggle, show }) => {
  return (
    <div className="footer">
      <div className="indicators">
        <span className="toggle" onClick={handleToggle}>
          <i className={show ? "ion-toggle" : "ion-toggle-filled"} />
        </span>
        <DividerVerticalSmall />
        {/*        <span className="sync-settings">
          <i className="ion-android-settings" />
        </span> */}
        <span className="price">
          XRC/USD <span className="xrc_fiat"> $23,89</span>
        </span>
        <DividerVerticalSmall />
        <span className="price">
          XRC/BTC <span className="xrc_btc"> 0.00012890</span>
        </span>
        <DividerVerticalSmall />
        <span className="price">
          XRC/LTC <span className="xrc_ltc"> 0.02897880</span>
        </span>
      </div>
      <div className="indicators">
        <span>
          <i className="ion-android-checkmark-circle" /> Tor is online
        </span>
        <DividerVerticalSmall />
        <span>
          <i className="ion-android-wifi" /> Peers: 7
        </span>
        <DividerVerticalSmall />
        <span>
          <i className="ion-ios-pulse-strong" /> BaseChain Height: 24 - Pool:
          0/0
        </span>
        <DividerVerticalSmall />
        <span>
          <i className="ion-ios-pulse-strong" /> MarketChain Height: 60 - Pool:
          0/0
        </span>{" "}
      </div>
    </div>
  );
};

export default Footer;
