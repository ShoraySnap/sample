import "./App.css";
import Theme from "./app/themes/Themes";
import RoutesContainer from "./app/routes";
import { createGlobalStyle } from "styled-components";
import { Suspense } from "react";

const GlobalStyle = createGlobalStyle`
  html, body, #root {
    color: ${(props) => props.theme.colors.primeBlack};
    font-family: Inter;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
    height: 100%;
  }
`;

function App() {
  return (
    <Theme>
      <GlobalStyle />
      <Suspense fallback={<span>Loading...</span>}>
        <RoutesContainer />
      </Suspense>
    </Theme>
  );
}

export default App;
