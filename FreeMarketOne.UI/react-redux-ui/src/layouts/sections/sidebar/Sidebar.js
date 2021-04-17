import React, { useState } from "react";

import menu from "../../../api/menu.json";

const Item = ({ item }) => {
  const [active, setActive] = useState(false);

  const handleActive = () => {
    setActive(!active);
  };

  return (
    <li onClick={handleActive}>
      <a href="#" className={active ? "selected" : ""}>
        <i className={item.icon} />
        <p>{item.title}</p>
      </a>
    </li>
  );
};
const Sidebar = ({ show }) => {
  return (
    <>
      <div id="sidebar-wrapper" className={show ? "toggled" : ""}>
        <ul className="sidebar-nav">
          {menu?.data?.map(item => {
            return <Item key={item.id} item={item} />;
          })}
        </ul>
      </div>
    </>
  );
};

export default Sidebar;
