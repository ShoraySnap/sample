import React from "react";
import { ROUTES } from "../constants";
import styled from "styled-components";
import { colors } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import _ from "lodash";
import { Checkbox } from "antd";
import sessionData from "../../services/sessionData";
import userPreferences from "../../services/userPreferences";

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
    border-top: 1px solid rgba(96, 129, 159, 0.2);
    justify-content: space-between;
  }
  .button-parent {
    display: flex;
    flex-direction: row;
  }
  .button-wrapper {
    z-index: 5;
  }
  .button-wrapper button {
    min-width: 9em;
    width: fit-content;
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

const Wrapper = styled.div`
  // position: relative;
  min-width: 100vw;
  max-height: 100%;
  display: flex;
  flex-direction: column;
  font-weight: 400;
  font-size: 14px;
  color: ${colors.primeBlack};
  overflow: auto;

  .content {
    display: flex;
    overflow: auto;
    flex-direction: column;
    padding: 1em 1em 5em 1em;
  }
`;

const WarningReconciliation = ({}) => {
  let showWarningAgain = true;
  const navigate = useNavigate();

  const onBack = () => {
    navigate(ROUTES.projectSelection);
  };

  const onSubmit = async () => {
    if (showWarningAgain == false) {
      window.electronAPI.updateUserPreferences(
        "showWarningReconciliation",
        showWarningAgain
      );
      userPreferences.set("showWarningReconciliation", showWarningAgain);
    }
    navigate(ROUTES.enterModelLink);
  };

  const leftButtonCallback = onBack;
  const rightButtonCallback = onSubmit;

  const onCheckbox = (e) => {
    showWarningAgain = !e.target.checked;
  };

  return (
    <ParentWrapper>
      <Wrapper>
        <div className="content">
          <p>
            The Revit model will directly export to Snaptrude without being
            reconciled with the existing model.
          </p>
        </div>
        <footer>
          <div className="page-indicator">
            <Checkbox onChange={onCheckbox}>Don't show again</Checkbox>
          </div>
          <div className="button-parent">
            <div className="button-wrapper">
              <Button
                customButtonStyle={{
                  backgroundColor: colors.fullWhite,
                  color: colors.secondaryGrey,
                }}
                title={"Back"}
                onPress={leftButtonCallback}
              />
            </div>
            <div className="button-wrapper">
              <Button
                primary={true}
                title={"I understand"}
                onPress={rightButtonCallback}
              />
            </div>
          </div>
        </footer>
      </Wrapper>
    </ParentWrapper>
  );
};

export default WarningReconciliation;
