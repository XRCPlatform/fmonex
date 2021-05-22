import React, { useEffect, useState } from "react";
import { useHistory } from "react-router-dom";

// Components
import Loading from "./components/Loading";
import LoginSignupSection from "./components/LoginSignupSection";

import "./Style.css";

const LoginSignup = () => {
  const history = useHistory();
  const [load, setLoad] = useState(false);

  useEffect(() => {
    if (load) {
      const timer = setTimeout(() => {
        history.push("/");
      }, 7000);
      return () => clearTimeout(timer);
    }
  }, [load]);

  return (
    <>
      <div className="box_c">
        {!load ? <LoginSignupSection setLoad={setLoad} /> : <Loading />}
      </div>
      <div className="footer_load">
        <div className="logo">
          free<span>market</span>one
        </div>
      </div>
    </>
  );
};

export default LoginSignup;
