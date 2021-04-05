import React, { useEffect, useRef, useState } from "react";

import "./Input.css";

const Select = ({ value, options, onChange }) => {
  const selectRef = useRef(null);
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    const handleClickOutside = e => {
      const isOutsideClick =
        selectRef.current && !selectRef.current.contains(e.target);
      if (isOutsideClick && isOpen) {
        setIsOpen(false);
      }
    };
    document.addEventListener("click", handleClickOutside);

    return () => {
      document.removeEventListener("click", handleClickOutside);
    };
  }, [isOpen]);

  const handleClick = () => {
    setIsOpen(!isOpen);
  };

  const handleChange = key => {
    onChange(key);
    handleClick();
  };

  return (
    <div className="select" ref={selectRef}>
      <div className="selection" onClick={handleClick}>
        <div className={isOpen ? "label open" : "label"}>{value.name}</div>
        <div className={isOpen ? "chevron open" : "chevron"}>
          {!isOpen ? (
            <i className="ion-android-arrow-dropdown"> </i>
          ) : (
            <i className="ion-android-arrow-dropup"> </i>
          )}
        </div>
      </div>
      <Options
        items={options}
        selected={value}
        onChange={handleChange}
        isOpen={isOpen}
      />
    </div>
  );
};

const Options = ({ items, selected, onChange, isOpen }) => (
  <ul className={isOpen ? "options open" : "options"}>
    {isOpen &&
      items?.map(item => (
        <li
          className={selected === item.id ? "option selected" : "option"}
          onClick={() => onChange(item)}
          key={item.id}
        >
          {item.name}
        </li>
      ))}
  </ul>
);

export default Select;
