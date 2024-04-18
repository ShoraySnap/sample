import React from "react";
import styled from "styled-components";
import loaderAnimation from "../../assets/loader-snaptrude.gif";
const Wrapper = styled.div`
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: transparent;
  font-size: 4em;
  img {
    width: 1em;
  }
`;
function LoadingScreen({ style, ...props }) {
  return (
    <Wrapper style={style} {...props}>
      <img
        src={loaderAnimation}
        alt="Loading"
        style={{ width: "150px", height: "150px" }}
      />
    </Wrapper>
  );
}

export default LoadingScreen;
