import React, { useEffect, useState, useRef } from "react";
import snaptrudeService from "../../services/snaptrude.service";
import { ROUTES } from "../constants";
import styled from "styled-components";
import { colors, fontSizes } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import urls from "../../services/urls";
import { Input, Button } from "antd";
import {
  LinkOutlined,
  LoadingOutlined,
  CheckCircleFilled,
  CloseOutlined,
} from "@ant-design/icons";
import { INPUT_FIELD_STATUS } from "../../services/constants";
import ProjectPreview from "./ProjectPreview";

const ModelValidatorWrapper = styled.div`
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
    padding: 0em 8em 1em 8em;
    align-items: start;
    line-height: 0.5rem;
  }
`;

function validateURL(inputText) {
  let domain = urls.get("snaptrudeReactUrl");

  if (domain.substring(0, 8) == "https://") {
    if (inputText.substring(0, 8) != "https://")
      inputText = "https://" + inputText;
  } else if (domain.substring(0, 7) == "http://") {
    if (inputText.substring(0, 7) != "http://")
      inputText = "http://" + inputText;
  }

  if (domain.slice(-1) != "/") {
    domain += "/";
  }

  var domainPath = domain + "model/";
  var pattern = new RegExp("^" + domainPath + "\\w{6}/?$");

  if (pattern.test(inputText)) {
    return INPUT_FIELD_STATUS.loading;
  }

  let floorKey = inputText.split("/model/")[1];
  let invalidPattern = inputText.match(/model/g).length;
  if (floorKey) {
    if (
      invalidPattern > 1 ||
      (floorKey.endsWith("/") && floorKey.length > 7) ||
      (!floorKey.endsWith("/") && floorKey.length > 6)
    ) {
      return INPUT_FIELD_STATUS.errorInvalid;
    }
  }

  return INPUT_FIELD_STATUS.blank;
}

const ModelValidator = ({}) => {
  const navigate = useNavigate();

  const onBack = () => {
    navigate(ROUTES.projectTypeSelection);
  };

  const onSubmit = async () => {
    window.electronAPI.uploadToExistingProject(modelURL);

    if (!modelURL) {
      window.electronAPI.operationFailed();
    }
    navigate(ROUTES.loading);
  };

  const leftButtonCallback = onBack;
  const rightButtonCallback = onSubmit;

  const [errorMessage, setErrorMessage] = useState("\u3000");
  const [modelURL, setModelURL] = useState("");
  const [status, setStatus] = useState(INPUT_FIELD_STATUS.blank);
  const [imageURL, setImageURL] = useState("");
  const [projectName, setProjectName] = useState("");
  const inputRef = useRef(null);

  const handleInputChange = (event) => {
    const newText = event.target.value;
    setErrorMessage("\u3000");

    const _status = validateURL(newText);
    setStatus(_status);
    if (_status == INPUT_FIELD_STATUS.errorInvalid) {
      setErrorMessage("Invalid URL");
    }

    setModelURL(newText);
  };

  const checkUrl = async () => {
    let floorKey = modelURL.endsWith("/")
      ? modelURL.slice(-7).slice(0, 6)
      : modelURL.slice(-6);
    const response = await snaptrudeService.checkModelUrl(floorKey);
    return response;
  };

  useEffect(() => {
    if (status != INPUT_FIELD_STATUS.loading) return;
    checkUrl().then((response) => {
      if (response?.status == 200 && response?.data != null) {
        if (response.data.access == true) {
          setStatus(INPUT_FIELD_STATUS.success);
          setImageURL(
            urls.get("snaptrudeDjangoUrl") + "/media/" + response.data.image
          );
          setProjectName(response.data.name);
        } else {
          setStatus(INPUT_FIELD_STATUS.errorInvalid);
          setErrorMessage(response.data.message);
        }
      } else {
        setStatus(INPUT_FIELD_STATUS.errorInvalid);
        setErrorMessage("Network error occurred");
      }
    });
  }, [modelURL]);

  useEffect(() => {
    inputRef.current.focus({
      cursor: "all",
    });
  }, []);

  return (
    <ModelValidatorWrapper>
      <div className="main-content">
        <p style={{ color: colors.Neutral[450], fontSize: fontSizes.tiny }}>
          Enter project URL:
        </p>

        <Input
          placeholder="Paste link here"
          value={modelURL}
          prefix={<LinkOutlined className="site-form-item-icon" />}
          suffix={
            status == INPUT_FIELD_STATUS.blank ? (
              <div />
            ) : status == INPUT_FIELD_STATUS.loading ? (
              <LoadingOutlined style={{ color: "rgba(0,0,0,.45)" }} />
            ) : status == INPUT_FIELD_STATUS.success ? (
              <CheckCircleFilled style={{ color: "#1A5EE5" }} />
            ) : (
              <CloseOutlined
                onClick={() => {
                  setErrorMessage("\u3000");
                  setStatus(INPUT_FIELD_STATUS.blank);
                  setModelURL("");
                }}
              />
            )
          }
          onChange={handleInputChange}
          status={errorMessage == "\u3000" ? "" : "error"}
          ref={inputRef}
          onFocus={() => {
            inputRef.current.focus({
              cursor: "all",
            });
          }}
        />
        <div style={{ height: "1em" }}></div>
        {status == INPUT_FIELD_STATUS.success && (
          <ProjectPreview imageURL={imageURL} projectName={projectName} />
        )}

        {(status == INPUT_FIELD_STATUS.errorAccess ||
          status == INPUT_FIELD_STATUS.errorInvalid) && (
          <div style={{ height: "4em" }}>
            <p
              style={{
                fontSize: "12px",
                color: "red",
                position: "relative",
                top: "-15px",
              }}
            >
              {errorMessage}
            </p>
          </div>
        )}

        {(status == INPUT_FIELD_STATUS.blank ||
          status == INPUT_FIELD_STATUS.loading) && (
          <div style={{ height: "4em" }}></div>
        )}
      </div>
      <footer>
        <div className="button-wrapper">
          <Button
            type="default"
            style={{
              borderColor: colors.fullWhite,
              color: colors.Neutral[600],
              marginRight: "1em",
            }}
            onClick={leftButtonCallback}
          >
            Back
          </Button>
          <Button
            type="default"
            disabled={status != INPUT_FIELD_STATUS.success}
            style={{
              background:
                status == INPUT_FIELD_STATUS.success ? colors.Neutral[900] : "",
              borderColor: colors.fullWhite,
              color:
                status == INPUT_FIELD_STATUS.success ? colors.fullWhite : "",
            }}
            onClick={rightButtonCallback}
          >
            Begin export
          </Button>
        </div>
      </footer>
    </ModelValidatorWrapper>
  );
};

export default ModelValidator;
