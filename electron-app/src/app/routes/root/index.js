import React, {useEffect} from "react";
import {Column, Container} from "../../components/Styled/Comps";
import Logo from "../../components/Snaptrude/Logo";
import {useNavigate} from "react-router-dom";
import {ROUTES} from "../constants";

const Root = (props) => {
  
  const navigate = useNavigate();
  
  useEffect(() => {
    window.electronAPI.goHome((event) => {
      navigate(ROUTES.home);
    });
  });
  
  return (
    <Container>
      <Column>
        <Logo/>
        <h2> Snaptrude Manager </h2>
      </Column>
    </Container>
  );
}

export default Root;