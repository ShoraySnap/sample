import React, { useState, useEffect } from "react";
import styled from "styled-components";
import { colors } from "../../themes/constant";

const Wrapper = styled.div`
width: 90%;
padding: 0.5em 0em;
// font-family: DM Sans;
font-weight: 400;
font-size: 14px;
line-height: 18px;
`

const ProgressBar = (props) => {
  const { bgcolor, completed, text, setParentShowProgressBar } = props;
  const [percentage, setPercentage] = useState(0);

  // useEffect(() => {
    // const interval = setInterval(() => {
    //   setPercentage((prev) => {
    //     const newPercentage = prev <= 75 ? prev + 25 : prev;
    //     if (newPercentage === 100) {
    //       setParentShowProgressBar(false);
    //       clearInterval(interval);
    //     }
    //     return newPercentage;
    //   });
    // }, 4000);
    // return () => {
    //   clearInterval(interval);
    //   setPercentage(0);
    // };
  // }, []);

  const containerStyles = {
    width: "100%",
    height: "8px",
    backgroundColor: colors.lightGrey,
    borderRadius: 8,
    margin: "8px 0px",
  };

  const fillerStyles = {
    height: "100%",
    width: `${completed}%`,
    backgroundColor: colors.red,
    borderRadius: "inherit",
    textAlign: "right",
    transition: "width 0.1s ease-in-out",
  };

  const labelStyles = {
    padding: 5,
    color: "white",
    fontWeight: "bold",
  };

  return (
    <Wrapper style={{ width: "90%", padding: "0.5em 0em" }}>
      <div style={{ display: "flex" }}>
        <div style={{ flex: 1 }}> {text} </div>
        <div style={{ float: "right" }}> {`${typeof completed === "number" ? completed.toFixed(0) : completed}%`} </div>
      </div>
      <div style={containerStyles}>
        <div style={fillerStyles}>
          <span style={labelStyles} />
        </div>
      </div>
    </Wrapper>
  );
};

export default ProgressBar;
