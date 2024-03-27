import styled from "styled-components";
import { colors, fontSizes } from "../../themes/constant";
import React from "react";
import { Button } from "antd";
import { Checkbox } from "antd";
import { InfoCircleOutlined } from "@ant-design/icons";

const ParentWrapper = styled.div`
  display: flex;
  justify-content: center;
  align-items: center;
  position: fixed;
  top: 0;
  left: 0;
  bottom: 0;
  right: 0;
  z-index: 10;

  .heading {
    font-weight: 700;
    font-size: 24px;
    color: ${colors.primeBlack};
  }
  .subheading {
    font-weight: 400;
    font-size: 14px;
    color: ${colors.greyishBlack};
  }
  footer {
    position: fixed;
    left: 0;
    bottom: 0;
    right: 0;

    display: flex;
    flex-direction: row;
    z-index: 5;
    padding: 1em;
    justify-content: space-between;
  }
  .button-parent {
    display: flex;
    flex-direction: row;
  }
  .button-wrapper button {
    height: 120%;
  }
  .page-indicator {
    display: flex;
    justify-content: center;
    align-items: center;
  }
  .active {
    background: #818181;
  }
  .ant-checkbox-checked .ant-checkbox-inner {
    background-color: black !important;
    border-color: black;
  }
  .ant-checkbox-wrapper:hover .ant-checkbox-checked .ant-checkbox-inner {
    background-color: black !important;
    border-color: black !important;
  }
  .ant-checkbox-checked:hover .ant-checkbox-checked .ant-checkbox-inner {
    background-color: black !important;
    border-color: black !important;
  }
  .ant-checkbox-checked:after {
    border-color: black !important;
    animation-duration: 0s !important;
  }
  .ant-checkbox:hover::after {
    border-color: black !important;
    animation-duration: 0s !important;
  }
  .ant-checkbox:hover .ant-checkbox-inner {
    border-color: black !important;
    animation-duration: 0s !important;
  }
  .ant-checkbox {
    animation-duration: 0s !important;
  }
`;

const WarningWrapper = styled.div`
  height: 90%;
  width: 100%;
  padding: 1em;

  .content {
    display: flex;
    flex-direction: column;
    padding: 1em 2em 1em 2em;
    border: 1.5px solid ${colors.Neutral[200]};
    border-radius: 0.75rem;
    text-align: left;
    height: 70%;
    align-items: start;

    color: ${colors.primeBlack};
    font-weight: 400;
    font-size: 14px;
  }
`;

export const WarningTemplate = ({
  note,
  backButton,
  nextButton,
  onCheckbox,
}) => {
  return (
    <ParentWrapper>
      <WarningWrapper>
        <div className="content">
          <div style={{ marginBottom: "-1.25em" }}>
            <InfoCircleOutlined style={{ color: colors.Neutral[600] }} />
            <p
              style={{
                display: "inline-block",
                paddingLeft: "5px",
                color: colors.Neutral[600],
              }}
            >
              Note:
            </p>
          </div>
          <p style={{ fontSize: fontSizes.small, color: colors.Neutral[800] }}>
            {note}
          </p>
        </div>
      </WarningWrapper>
      <footer>
        <div className="page-indicator">
          <Checkbox
            onChange={onCheckbox}
            style={{
              color: colors.Neutral[600],
              fontWeight: 400,
            }}
          >
            Don't show again
          </Checkbox>
        </div>
        <div className="button-parent">
          <div className="button-wrapper">
            <Button
              type="default"
              style={{
                background: "#ffffff",
                borderColor: "white",
                color: colors.Neutral[600],
                marginRight: "1em",
              }}
              onClick={backButton}
            >
              Back
            </Button>
            <Button
              type="default"
              style={{
                background: colors.Neutral[900],
                borderColor: "white",
                color: "#ffffff",
              }}
              onClick={nextButton}
            >
              I understand
            </Button>
          </div>
        </div>
      </footer>
    </ParentWrapper>
  );
};
