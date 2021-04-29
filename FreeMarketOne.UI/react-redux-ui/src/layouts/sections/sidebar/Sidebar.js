import React from "react";
import { NavLink } from "react-router-dom";

import menu from "../../../api/menu.json";

const MenuLink = ({ item }) => (
  <li>
    <NavLink to={item.to} exact key={item.id} activeClassName="selected">
      <i className={item.icon} />
      <p>{item.title}</p>
    </NavLink>
  </li>
);

const Sidebar = ({ show }) => {
  return (
    <>
      <div id="sidebar-wrapper" className={show ? "toggled" : ""}>
        <ul className="sidebar-nav">
          {menu?.data?.map(item => {
            return <MenuLink key={item.id} item={item} />;
          })}
        </ul>
      </div>
    </>
  );
};

export default Sidebar;
