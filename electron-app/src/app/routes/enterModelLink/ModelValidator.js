import React, { useEffect, useState } from "react";
import snaptrudeService from "../../services/snaptrude.service";
import {
  ROUTES,
} from "../constants";
import styled from "styled-components";
import { colors } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import urls from "../../services/urls";
import _ from "lodash";
import { RouteStore } from "../routeStore";

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


const ModelValidator = ({
}) => {
  const navigate = useNavigate();

  const onBack = () => {
    navigate(ROUTES.projectSelection);
  };

  const onSubmit = async () => {
    window.electronAPI.uploadToExistingProject(modelCode);

    if (modelCode) {
      RouteStore.set("projectLink", urls.get("snaptrudeReactUrl") + "/model/" + modelCode);
    } else {
      // logger.log("Operation failed");
      window.electronAPI.operationFailed();
    }
    navigate(ROUTES.loading);
  };

  const leftButtonCallback = onBack;
  const rightButtonCallback = onSubmit;

  const heading = `Enter Model Link`;
  let isUrlValid = false;

  const [errorMessage, setErrorMessage] = useState('\u3000');
  const [modelCode, setModelCode] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isDisabled, setIsDisabled] = useState(true);

  const handleInputChange = (event) => {
    event.target.value = event.target.value.toUpperCase();
    const newText = event.target.value;
    setErrorMessage('\u3000');
    setIsDisabled(true);
    setModelCode(newText);

    if (newText.length == 6) {
      setIsLoading(true);
    }
    else {
      setIsLoading(false);
    }
  };

  const checkUrl = async () => {
    const url = urls.get("snaptrudeReactUrl") + "/model/" + modelCode;
    isUrlValid = await snaptrudeService.checkModelUrl(url);
  }

  useEffect(() => {
    checkUrl().then(() => {
      setIsLoading(false);

      if(modelCode.length != 6) return;

      if (isUrlValid) {
        setIsDisabled(false);
      }
      else {
        setErrorMessage("Invalid model link");
      }
    });
  }, [modelCode]);

  return (
    <Wrapper>
      <div className="content">
        <p>{heading}</p>
        <WorkspacesGrid>
          <div/>
          <p>http://localhost:3000/model/</p>
          <input 
            style={{lineHeight:"1.6rem"}} 
            placeholder="ADV4T7"
            onChange={handleInputChange}
            maxLength="6"
          />
        </WorkspacesGrid>
        <p style={{fontSize:"12px", color:"red"}}>{errorMessage}</p>
      </div>
      <footer>
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
            disabled={isDisabled}
            primary={true}
            title={"Next"}
            onPress={rightButtonCallback}
          />
        </div>
      </footer>
    </Wrapper>
  );
};

export default ModelValidator;
