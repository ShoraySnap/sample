import React from "react";
import styled from "styled-components";
import { colors, fontSizes } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import { ROUTES } from "../constants";
import { Button } from "antd";
import { FileOutlined, PlusOutlined } from "@ant-design/icons";
import userPreferences from "../../services/userPreferences";

const ProjectTypeSelectionWrapper = styled.div`
  footer {
    position: fixed;
    left: 0;
    bottom: 0;
    right: 0;

    display: flex;
    flex-direction: row;
    z-index: 5;
    padding: 1em;
    justify-content: flex-end;
    margin-right: 1em;
  }
  .content {
    display: flex;
    flex-direction: column;
    padding: 5em 0em 5em 0em;
  }
  .button-row {
    display: flex;
    flex-direction: row;
    gap: 0.5em;
    justify-content: center;
  }
  .button-row button {
    height: 2.4em;
  }
  .ant-btn {
    &::after {
      all: unset;
    }
  }
`;

const ProjectTypeSelection = (props) => {
  const navigate = useNavigate();
  const backButtonCallback = async () => {
    navigate(ROUTES.home);
  };
  return (
    <ProjectTypeSelectionWrapper>
      <div className="content">
        <p style={{ fontSize: fontSizes.tiny, fontWeight: "500" }}>
          {"Export model to"}
        </p>
        <div className="button-row">
          <Button
            type="primary"
            style={{ background: colors.Neutral[900] }}
            onClick={() => {
              navigate(ROUTES.chooseProjectLocation);
            }}
          >
            <PlusOutlined />
            New Project
          </Button>
          <Button
            type="primary"
            style={{ background: colors.Neutral[900] }}
            onClick={() => {
              userPreferences.get("showWarningReconciliation") == true
                ? navigate(ROUTES.warningReconciliation)
                : navigate(ROUTES.enterModelLink);
            }}
          >
            <FileOutlined />
            Existing Project
          </Button>
        </div>
      </div>
      <footer>
        <div className="button-parent">
          <div className="button-wrapper">
            <Button
              type="default"
              style={{
                background: "#ffffff",
                borderColor: "white",
                color: colors.Neutral[600],
              }}
              onClick={backButtonCallback}
            >
              Back
            </Button>
          </div>
        </div>
      </footer>
    </ProjectTypeSelectionWrapper>
  );
};

export default ProjectTypeSelection;
