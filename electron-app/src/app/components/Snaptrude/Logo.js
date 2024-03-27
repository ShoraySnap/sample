import logo from "../../assets/logo.png";
import React from "react";
import { LogoContainer } from "../Styled/Comps";

const Logo = (props) => {
  return (
    <LogoContainer>
      <img src={logo} alt="logo" style={{ width: "5em", height: "5em" }} />
    </LogoContainer>
  );
};

export default Logo;
