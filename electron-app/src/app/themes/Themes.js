import React from "react";
import { ThemeProvider } from "styled-components";
import PropTypes from "prop-types";
import { colors, fontSizes } from "./constant";

const theme = {
  colors,
  font: '"DM Sans"', // makes it look weird
  fontSizes,
  fontWeight: {
    normal: 500,
    bold: "bold",
    italic: "italic",
  },
};

const Theme = ({ children }) => (
  <ThemeProvider theme={theme}>{children}</ThemeProvider>
);

Theme.propTypes = {
  children: PropTypes.array.isRequired,
  // children: PropTypes.objectOf(PropTypes.any).isRequired,
};

export default Theme;
