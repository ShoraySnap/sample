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
import { Input } from "antd";
import {
  LinkOutlined,
  LoadingOutlined,
  CheckCircleFilled,
  CloseOutlined,
} from "@ant-design/icons";
import { INPUT_FIELD_STATUS } from "../../services/constants";

const Wrapper = styled.div`
  min-width: 100vw;
  max-height: 100%;
  display: flex;
  flex-direction: column;
  font-weight: 400;
  font-size: 14px;
  color: ${colors.primeBlack};
  overflow: auto;

  .main-content {
    display: flex;
    flex-direction: column;
    padding: 1em 8em 5em 8em;
    align-items: start;
    line-height: 0.5rem;
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

const ModelValidator = ({}) => {
  const navigate = useNavigate();

  const onBack = () => {
    navigate(ROUTES.projectSelection);
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
  const [status, setStatus] = useState(INPUT_FIELD_STATUS.blank);

  const handleInputChange = (event) => {
    event.target.value = event.target.value.toUpperCase();
    const newText = event.target.value;
    setErrorMessage("\u3000");
    setModelCode(newText);

    if (newText.length == 6) {
      setStatus(INPUT_FIELD_STATUS.loading);
    } else {
      setStatus(INPUT_FIELD_STATUS.blank);
    }
  };

  const checkUrl = async () => {
    const isUrlValid = await snaptrudeService.checkModelUrl(modelCode);
    return isUrlValid;
  };

  useEffect(() => {
    if (modelCode.length != 6) return;
    checkUrl().then((isUrlValid) => {
      if (isUrlValid) {
        setStatus(INPUT_FIELD_STATUS.success);
      } else {
        setStatus(INPUT_FIELD_STATUS.errorInvalid);
        setErrorMessage("Invalid model link");
      }
    });
  }, [modelCode]);

  return (
    <Wrapper>
      <div className="main-content">
        <p>Enter project URL:</p>
        <Input
          placeholder="Paste link here"
          prefix={<LinkOutlined className="site-form-item-icon" />}
          suffix={
            status == INPUT_FIELD_STATUS.blank ? (
              <div />
            ) : status == INPUT_FIELD_STATUS.loading ? (
              <LoadingOutlined style={{ color: "rgba(0,0,0,.45)" }} />
            ) : status == INPUT_FIELD_STATUS.success ? (
              <CheckCircleFilled style={{ color: "#1A5EE5" }} />
            ) : (
              <CloseOutlined />
            )
          }
          onChange={handleInputChange}
          status={errorMessage == "\u3000" ? "" : "error"}
        />
        <p style={{ fontSize: "12px", color: "red" }}>{errorMessage}</p>
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
            disabled={status != INPUT_FIELD_STATUS.success}
            primary={true}
            title={"Begin export"}
            onPress={rightButtonCallback}
          />
        </div>
      </footer>
    </Wrapper>
  );
};

export default ModelValidator;
