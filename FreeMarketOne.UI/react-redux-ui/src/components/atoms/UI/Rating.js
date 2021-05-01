import React from "react";

import DefaultUserPic from "Assets/images/defaultuser.jpg";
import "./Ui.css";

const Rating = () => {
  return (
    <>
      <div className="sidebar-header">
        <div className="user-pic">
          <img
            className="img-responsive img-rounded"
            src={DefaultUserPic}
            alt=""
          />
        </div>
        <div className="user-info">
          <span>Hello, Rulez</span>

          <div className="rating-star">
            <i className="ion-ios-star" />
            <i className="ion-ios-star" />
            <i className="ion-ios-star" />
            <i className="ion-ios-star-half" />
            <i className="ion-ios-star-outline" />
          </div>
          {/*           <span className="user-role">
            <i className="ion-ribbon-a" />
            <i className="ion-ribbon-b" />
            <i className="ion-bookmark" />
              Administrator 
          </span> */}
        </div>
      </div>
    </>
  );
};

/* Rating.defaultProps = {
  
};
 */
export default Rating;
