import React, { useEffect, useState } from "react";
import snaptrudeService from "../../services/snaptrude.service";
import { ROUTES } from "../constants";
import styled from "styled-components";
import { colors } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import urls from "../../services/urls";
import _ from "lodash";
import { RouteStore } from "../routeStore";
import { Checkbox } from "antd";
// import electronCommunicator from "../../../electron/communicator";

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

const WorkspacesGrid = styled.div`
  display: grid;
  grid-template-rows: 40px 40px 35px;
  grid-template-columns: 10% 40% 25% 25%;
  margin-top: 20px;
  overflow: auto;
  align-items: center;
`;

const WarningVisibility = ({}) => {
  const navigate = useNavigate();

  const onBack = () => {
    navigate(ROUTES.home);
  };

  const onSubmit = async () => {
    window.electronAPI.uploadToExistingProject(modelCode);

    if (modelCode) {
      RouteStore.set(
        "projectLink",
        urls.get("snaptrudeReactUrl") + "/model/" + modelCode
      );
    } else {
      // logger.log("Operation failed");
      window.electronAPI.operationFailed();
    }
    navigate(ROUTES.loading);
  };

  const leftButtonCallback = onBack;
  const rightButtonCallback = onSubmit;

  const heading = `Enter Model Link`;

  const [errorMessage, setErrorMessage] = useState("\u3000");
  const [modelCode, setModelCode] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isDisabled, setIsDisabled] = useState(true);

  const handleInputChange = (event) => {
    event.target.value = event.target.value.toUpperCase();
    const newText = event.target.value;
    setErrorMessage("\u3000");
    setIsDisabled(true);
    setModelCode(newText);

    if (newText.length == 6) {
      setIsLoading(true);
    } else {
      setIsLoading(false);
    }
  };

  const checkUrl = async () => {
    const isUrlValid = await snaptrudeService.checkModelUrl(modelCode);
    return isUrlValid;
  };

  const onCheckbox = (e) => {
    window.electronAPI.updateUserPreferences(
      "showWarningVisibility",
      !e.target.checked
    );
  };

  useEffect(() => {
    setIsLoading(false);
    if (modelCode.length != 6) return;
    checkUrl().then((isUrlValid) => {
      if (isUrlValid) {
        setIsDisabled(false);
      } else {
        setErrorMessage("Invalid model link");
      }
    });
  }, [modelCode]);

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
                isLoading={isLoading}
                disabled={false}
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

export default WarningVisibility;
