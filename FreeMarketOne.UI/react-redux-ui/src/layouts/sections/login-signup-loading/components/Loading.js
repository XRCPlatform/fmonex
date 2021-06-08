import React from "react";

import "../Style.css";

const Loading = () => {
  return (
    <>
      <div className="header_load">
        <div className="loading_msg">
          <div className="textChange__container">
            <div className="textChange__sprite">
              <b>
                Connecting to xRhodium network<span>...</span>{" "}
              </b>
              <b>
                Connecting to Tor network (1/3)<span>...</span>{" "}
              </b>
              <b>
                Connecting to Tor network (2/3)<span>...</span>
              </b>
              <b>
                Connecting to Tor network (3/3)<span>...</span>
              </b>
              <b>
                Loading Tor manager<span>...</span>{" "}
              </b>
              <b>
                Loading main blockchain (1/3)<span>...</span>{" "}
              </b>
              <b>
                Loading main blockchain (2/3)<span>...</span>
              </b>
              <b>
                Loading main blockchain (3/3)<span>...</span>
              </b>
              <b>
                Connecting to Bitcoin network<span>...</span>{" "}
              </b>
              <b>
                Connecting to Bitcoin network (1/2)<span>...</span>{" "}
              </b>
              <b>
                Connecting to Bitcoin network (2/2)<span>...</span>
              </b>
              <b>
                Connected<span>...</span>
              </b>
            </div>
          </div>
        </div>
        <ul className="fog">
          <li />
          <li />
          <li />
          <li />
          <li />
        </ul>
      </div>
    </>
  );
};

export default Loading;
