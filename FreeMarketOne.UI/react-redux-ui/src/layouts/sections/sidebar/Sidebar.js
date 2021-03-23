import React from "react";

import DividerHorizontal from "Components/atoms/UI/DividerHorizontal";

const Sidebar = ({ show }) => {
  return (
    <>
      <div id="sidebar-wrapper" className={show ? "toggled" : ""}>
        <ul className="sidebar-nav">
          <li className="sidebar-brand">
            <span>Hello, Rulez</span>
          </li>
          <DividerHorizontal gradient />
          <li>
            <a href="#" className="active">
              <i className="ion-ios-home-outline" />
            </a>
          </li>
          <li>
            <a href="#">
              <i className="ion-stats-bars" />
              <p>All markets</p>
            </a>
          </li>
          <li>
            <a href="#">
              <i className="ion-ios-list-outline" />
              <p>Categories</p>
            </a>
          </li>
          <li>
            <a href="#">
              <i className="ion-ios-albums-outline" />
              <p>My products</p>
            </a>
          </li>
        </ul>
      </div>
    </>
  );
};

export default Sidebar;
