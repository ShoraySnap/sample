import styled from "styled-components";

export const Container = styled.div`
  text-align: center;
`;

export const LogoContainer = styled.div`
  height: 6em;
  pointer-events: none;
`;

export const Column = styled.div`
  background-color: #ffffff;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  font-size: calc(10px + 2vmin);
`;

export const Rows = styled.div`
  background-color: #ffffff;
  min-width: 100vh;
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: center;
  font-size: calc(10px + 2vmin);
`;