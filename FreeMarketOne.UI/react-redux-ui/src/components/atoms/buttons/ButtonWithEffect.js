import React from "react";
import "./Buttons.css";

const ButtonWithEffect = props => {
  const { title, onClick } = props;
  return (
    <>
      <div className="btn_box">
        <button
          type="button"
          className="btn-animate ripple"
          data-ripple-color="#ffffff"
          onClick={e => {
            onClick(e);
          }}
        >
          <svg>
            <rect className="shape" height="30" width="110" />
          </svg>
          <span>{title}</span>
        </button>
      </div>
    </>
  );
};

ButtonWithEffect.defaultProps = {
  onClick: () => console.log("Clicked!")
};

export default ButtonWithEffect;
