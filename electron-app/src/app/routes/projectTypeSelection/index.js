import React from "react";
import styled from "styled-components";
import { colors } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import { ROUTES } from "../constants";
import Button from "../../components/Button";
import userPreferences from "../../services/userPreferences";

const Wrapper = styled.div`
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
  padding: 0em 2em 0em 2em;
  .content {
    display: flex;
    flex-direction: column;
    padding: 5em 0em 5em 0em;
  }
  .button-row {
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    margin-left: 7em;
    margin-right: 7em;
    gap: 1em;
  }
`;

const ProjectTypeSelection = (props) => {
  const navigate = useNavigate();
  const backButtonCallback = async () => {
    navigate(ROUTES.home);
  };
  return (
    <Wrapper>
      <div className="content">
        <p>{"Export model to"}</p>
        <div className="button-row">
          <Button
            weight={500}
            primary={true}
            title={"New Project"}
            onPress={() => {
              navigate(ROUTES.chooseProjectLocation);
            }}
          />
          <Button
            className="button"
            weight={500}
            primary={true}
            title={"Existing Project"}
            onPress={() => {
              userPreferences.get("showWarningReconciliation") == true
                ? navigate(ROUTES.warningReconciliation)
                : navigate(ROUTES.enterModelLink);
            }}
          />
        </div>
      </div>
      <footer>
        <div className="button-parent">
          <div className="button-wrapper">
            <Button
              customButtonStyle={{
                backgroundColor: colors.fullWhite,
                color: colors.secondaryGrey,
              }}
              title={"Back"}
              onPress={backButtonCallback}
            />
          </div>
        </div>
      </footer>
    </Wrapper>
  );
};

export default ProjectTypeSelection;
