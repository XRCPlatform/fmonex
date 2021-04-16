import React from "react";
import "./Ui.css";

const Rating = () => {
  return (
    <>
      <span>Hello, Rulez</span>

      <div className="rating-star">
        <i className="ion-ios-star" />
        <i className="ion-ios-star" />
        <i className="ion-ios-star" />
        <i className="ion-ios-star-half" />
        <i className="ion-ios-star-outline" />
      </div>
    </>
  );
};

/* Rating.defaultProps = {
  
};
 */
export default Rating;
