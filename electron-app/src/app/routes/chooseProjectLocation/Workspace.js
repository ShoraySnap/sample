import React, { useEffect, useState } from "react";
import snaptrudeService from "../../services/snaptrude.service";
import sessionData from "../../services/sessionData";
import { ROUTES } from "../constants";
import styled from "styled-components";
import { colors } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import team from "../../assets/team.svg";
import personal from "../../assets/personal.svg";
import logger from "../../services/logger";
import LoadingScreen from "../../components/Loader";
import urls from "../../services/urls";

const Wrapper = styled.div`
  // position: relative;
  min-width: 100vw;
  display: flex;
  flex-direction: column;
  font-weight: 400;
  font-size: 14px;
  color: ${colors.primeBlack};

  .content {
    // overflow: auto;
    padding: 1em 1em 5em 1em;
  }
`;

const WorkspacesGrid = styled.div`
  display: grid;
  grid-template-rows: 40px 40px 40px 40px;
  grid-template-columns: 45vw 45vw;

  margin-top: 40px;

  // overflow-y: scroll;
`;

const WorkspaceInfo = styled.div`
  display: flex;
  flex-direction: row;
  justify-content: left;
  // align-items: center;

  margin-left: 30px;

  border-radius: 0.5rem;

  &:hover {
    background-color: #f2f2f2;
    cursor: pointer;
    font-weight: 500;
  }

  .selected-img {
    filter: invert(26%) sepia(94%) saturate(4987%) hue-rotate(337deg)
      brightness(93%) contrast(98%);
  }

  .selected-txt {
    font-weight: 600;
  }
`;

const WorkspaceIcon = styled.img`
  padding: 6px;
  padding-right: 2px;
  // width: 16px;
  // height: 14px;
`;

const WorkspaceTitle = styled.p`
  font-weight: 500;
  font-size: 14px;
`;


const Workspace = () => {
  const navigate = useNavigate();

  const goHome = () => {
    navigate(ROUTES.home);
  };

  const closeApplication = () =>{
    window.electronAPI.closeApplication();
  }

  const onSubmit = async () => {
    setIsLoading(true);
    const projectLink = await snaptrudeService.createProject(
      sessionData.getUserData()["streamId"],
      selectedWorkspace
    );
    // const projectLink = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

    setIsLoading(false);

    if (projectLink) {
      window.electronAPI.openPageInDefaultBrowser(projectLink);
      window.electronAPI.operationSucceeded();
    } else {
      // logger.log("Operation failed");
      window.electronAPI.operationFailed();
    }
    goHome();
  };

  const onSubmitGoToPayment = async () => {
    const upgradePlanLink =
      urls.get("snaptrudeReactUrl") + "/dashboard/profile/billing";

    if (upgradePlanLink) {
      window.electronAPI.openPageInDefaultBrowser(upgradePlanLink);
      window.electronAPI.operationSucceeded();
    } else {
      // logger.log("Operation failed");
      window.electronAPI.operationFailed();
    }
    goHome();
  };

  const onWorkspaceClick = (id) => {
    setSelectedWorkspace(id);
  };
  
  const [selectedWorkspace, setSelectedWorkspace] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isWorkSpaceLoading, setIsWorkSpaceLoading] = useState(true);

  const [workspaces, setWorkspaces] = useState([]);

  useEffect(() => {
    const getWorkspaces = async () => {
      let userWorkspaces = await snaptrudeService.getUserWorkspaces();

      if (userWorkspaces) {
        userWorkspaces.forEach((space) => (space.icon = team));

        const personalWorkspace = {
          id: "",
          name: "My Workspace",
          icon: personal,
        };

        const isValidPersonalWorkspace =
          await snaptrudeService.checkPersonalWorkspaces();
        if (isValidPersonalWorkspace) {
          userWorkspaces.unshift(personalWorkspace);
        }

        if (userWorkspaces.length) {
          setWorkspaces(userWorkspaces);
        }
      }
    };
    getWorkspaces().then(() => {
      setIsWorkSpaceLoading(false);
    });
  }, []);

  if (isWorkSpaceLoading) return <LoadingScreen />;
  if (!workspaces.length)
    return (
      <>
        <p>
          You’re currently on Free plan, please upgrade to continue using
          Snaptrude
        </p>
        <footer>
        <div className="button-wrapper">
          <Button
            customButtonStyle={{
              backgroundColor: colors.fullWhite,
              color: colors.secondaryGrey,
            }}
            title={"Cancel"}
            onPress={closeApplication}
          />
        </div>
        <div className="button-wrapper">
          <Button
            primary={true}
            title={"Upgrade Plan"}
            onPress={onSubmitGoToPayment}
          />
        </div>
      </footer>
      </>
    );
  return (
    <Wrapper>
      <div className="content">
        <p>Select the workspace to upload to</p>
        <WorkspacesGrid>
          {workspaces.map(({ id, name, icon }, idx) => {
            const className = id === selectedWorkspace ? "selected" : "";
            return (
              <WorkspaceInfo key={idx} onClick={() => onWorkspaceClick(id)}>
                <WorkspaceIcon
                  className={className + "-img"}
                  src={icon}
                  alt={"workspace"}
                />
                <WorkspaceTitle className={className + "-txt"}>
                  {" "}
                  {name}{" "}
                </WorkspaceTitle>
              </WorkspaceInfo>
            );
          })}
        </WorkspacesGrid>
      </div>
      <footer>
        <div className="button-wrapper">
          <Button
            customButtonStyle={{
              backgroundColor: colors.fullWhite,
              color: colors.secondaryGrey,
            }}
            title={"Cancel"}
            onPress={closeApplication}
          />
        </div>
        <div className="button-wrapper">
          <Button
            isLoading={isLoading}
            primary={true}
            title={"Done"}
            onPress={onSubmit}
          />
        </div>
      </footer>
    </Wrapper>
  );
};

export default Workspace;
