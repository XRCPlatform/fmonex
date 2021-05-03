import React from "react";
import "./Ui.css";

const Divider = props => {
  const { razor, gradient } = props;
  return (
    <>
      {gradient && <span className="divider gradient" contentEditable />}

      {razor && <span className="divider line razor" contentEditable />}
    </>
  );
};

Divider.defaultProps = {
  gradient: false,
  razor: false
};

export default Divider;
