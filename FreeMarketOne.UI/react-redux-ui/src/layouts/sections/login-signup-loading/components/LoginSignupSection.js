import React, { useState } from "react";
import { useDispatch } from "react-redux";
import { useHistory } from "react-router-dom";

// Actions
import { login } from "Modules/units/Auth";

import Button from "../../../../components/atoms/buttons/ButtonWithEffect";

import "../Style.css";

const LoginSignupSection = ({ setLoad }) => {
  const dispatch = useDispatch();
  const history = useHistory();

  const [state, setState] = useState({ check: "login" });

  const handleChange = e => {
    const { name, value } = e.target;

    setState({
      [name]: value
    });
  };

  const handleSubmit = e => {
    e.preventDefault();

    dispatch(login(history, setLoad));
  };

  return (
    <>
      <div className="login">
        <div className="login_title">
          {state.check !== "login" ? (
            <span>Create an account</span>
          ) : (
            <span>Login to your account</span>
          )}
        </div>
        <div className="switch">
          <div className="input-row">
            <input
              type="radio"
              className="switch-input"
              name="check"
              value="login"
              id="login"
              checked={state.check === "login"}
              onChange={handleChange}
            />
            <label htmlFor="login" className="switch-label switch-label-off">
              Login
            </label>
          </div>
          <input
            type="radio"
            className="switch-input"
            name="check"
            value="signup"
            id="signup"
            checked={state.check === "signup"}
            onChange={handleChange}
          />
          <label htmlFor="signup" className="switch-label switch-label-on">
            Signup
          </label>
          <span className="switch-selection" />
        </div>
        <div className="login_fields">
          <div className="login_fields__user">
            <div className="icon">
              <i className="ion-ios-contact-outline" />
            </div>
            <input placeholder="Username" type="text" />
            <div className="validation" />
          </div>
          <div className="login_fields__password">
            <div className="icon">
              <i className="ion-ios-locked-outline" />
            </div>
            <input placeholder="Password" type="password" />
            <div className="validation" />
          </div>

          {state.check !== "login" && (
            <div className="login_fields__rptpassword">
              <div className="icon">
                <i className="ion-ios-locked-outline" />
              </div>
              <input placeholder="Repeat password" type="password" />
              <div className="validation" />
            </div>
          )}
          <div className="login_fields__submit">
            <Button
              title={state.check !== "login" ? "Signup" : "Login"}
              onClick={handleSubmit}
            />
          </div>
        </div>
        <div className="disclaimer">
          <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce
            semper laoreet placerat. Nullam semper auctor justo, rutrum posuere
            odio vulputate nec.
          </p>
        </div>
      </div>
    </>
  );
};

export default LoginSignupSection;
