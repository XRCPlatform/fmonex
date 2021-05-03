import React from "react";
import { NavLink } from "react-router-dom";

import menu from "../../../api/menu.json";

import "./Sidebar.css";

const MenuLink = ({ item }) => {
  return (
    <li>
      <NavLink
        className="menu-a"
        to={item.to}
        exact
        key={item.id}
        activeClassName="selected"
      >
        <i className={item.icon} />
        {!item?.subMenu.length > 0 && (
          <span className="tooltip">{item.title}</span>
        )}
      </NavLink>

      {item?.subMenu.length > 0 && (
        <ul className="nav-flyout">
          <div className="submenu-title">
            <i className={item.icon} />
            {item.title}
          </div>
          {item?.subMenu?.map((subMenu, index) => {
            return (
              <li key={subMenu.title + index}>
                <NavLink to={item.to} exact activeClassName="selected">
                  <span>
                    <i className="ion-android-radio-button-off" />
                    {subMenu.title}
                  </span>
                </NavLink>
              </li>
            );
          })}
          <div id="background-small-logo">
            <p id="small-logo-text">FM1</p>
          </div>
        </ul>
      )}
    </li>
  );
};

const Sidebar = ({ show }) => {
  return (
    <div
      id="sidebar-wrapper"
      className={show ? "sidebar-nav toggled" : "sidebar-nav"}
    >
      <ul>
        {menu?.data?.map(item => {
          return <MenuLink key={item.id} item={item} />;
        })}
      </ul>
    </div>
  );
};

export default Sidebar;
