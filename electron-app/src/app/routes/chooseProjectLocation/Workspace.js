import React, { useEffect, useState } from "react";
import snaptrudeService from "../../services/snaptrude.service";
import sessionData from "../../services/sessionData";
import {
  PERSONAL_WORKSPACE_ID,
  PERSONAL_WORKSPACE_NAME,
  ROOT_FOLDER_ID,
  ROUTES,
} from "../constants";
import styled from "styled-components";
import { colors } from "../../themes/constant";
import { useNavigate } from "react-router-dom";
import Button from "../../components/Button";
import team from "../../assets/team.svg";
import personal from "../../assets/personal.svg";
import folder from "../../assets/folder.svg";
import logger from "../../services/logger";
import LoadingScreen from "../../components/Loader";
import urls from "../../services/urls";
import _ from "lodash";
import UpgradePlan from "../../components/UpgradePlan";
import { Tooltip } from "antd";
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
  grid-template-rows: 40px 40px 40px 40px;
  grid-template-columns: 45vw 45vw;
  margin-top: 20px;
  overflow: auto;
`;

const WorkspaceInfo = styled.div`
  display: flex;
  flex-direction: row;
  justify-content: left;
  align-items: center;
  margin-left: 30px;
  flex-flow: row;

  border-radius: 0.5rem;

  &:hover {
    background-color: #f2f2f2;
    cursor: pointer;
    font-weight: 500;
  }

  [class*="folder"] {
    // has this string somewhere
    padding: 8px;
  }

  [class$="selected-img"] {
    // ends with this string
    filter: invert(26%) sepia(94%) saturate(4987%) hue-rotate(337deg)
      brightness(93%) contrast(98%);
  }

  [class$="selected-txt"] {
    font-weight: 600;
  }
`;

const WorkspaceIcon = styled.img`
  padding: 2px;
  padding-right: 2px;
`;

const WorkspaceTitle = styled.p`
  font-weight: 500;
  font-size: 14px;
  text-overflow: ellipsis;
  overflow: hidden;
  white-space: nowrap;
`;

const CSS_FOLDER_TAG = "folder-";
const CSS_WORKSPACE_TAG = "";

const Workspace = ({
  selectedWorkspaceId,
  setSelectedWorkspaceId,
  selectedWorkspaceName,
  setSelectedWorkspaceName,
  foldersArray,
  setFoldersArray,
}) => {
  const navigate = useNavigate();

  const goHome = () => {
    navigate(ROUTES.home);
  };

  const closeApplication = () => {
    window.electronAPI.closeApplication();
  };

  const onSubmit = async () => {
    setIsLoading(true);

    const workspaceId = selectedWorkspaceId;
    const folderId = currentFolderId;

    // const projectLink = await snaptrudeService.createProject(
    //   sessionData.getUserData()["streamId"],
    //   workspaceId,
    //   folderId
    // );
    // const projectLink = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

    setIsLoading(false);
    window.electronAPI.uploadToSnaptrude(workspaceId, folderId);
    const floorKey = sessionData.getUserData()["floorkey"];
    const projectLink = urls.get("snaptrudeReactUrl") + "/model/" + floorKey;

    if (projectLink) {
      RouteStore.set("projectLink", projectLink);
    } else {
      // logger.log("Operation failed");
      window.electronAPI.operationFailed();
    }
    navigate(ROUTES.loading);
  };

  const onSubmitGoToPayment = async () => {
    const upgradePlanLink =
      urls.get("snaptrudeReactUrl") + "/dashboard/profile/plans";

    if (upgradePlanLink) {
      // MAKE CHANGES HERE TOO
      window.electronAPI.openPageInDefaultBrowser(upgradePlanLink);
      window.electronAPI.operationSucceeded();
    } else {
      // logger.log("Operation failed");
      window.electronAPI.operationFailed();
    }
    goHome();
  };

  const onWorkspaceClick = (workspaceId, workspaceName) => {
    setSelectedEntryId(workspaceId);
    setSelectedEntryName(workspaceName);
  };

  const onFolderClick = (folderId, folderName) => {
    if (currentFolderId === folderId) return;

    setSelectedEntryId(folderId);
    setFoldersArray([...foldersArray, { id: folderId, name: folderName }]);
  };

  const goBackToWorkspaces = () => {
    setSelectedWorkspaceId(null);
    setSelectedEntryId(selectedWorkspaceId);
    setFoldersArray([{ id: ROOT_FOLDER_ID, name: "" }]);
  };

  const goOneFolderUp = () => {
    setSelectedEntryId(parentFolderId);
    foldersArray.pop();
    setFoldersArray([...foldersArray]);
  };

  const chooseWorkspace = () => {
    setSelectedWorkspaceId(selectedEntryId);
    setSelectedWorkspaceName(selectedEntryName);
    setSelectedEntryId(ROOT_FOLDER_ID);
  };

  const getWorkspaces = async () => {
    let userWorkspaces = await snaptrudeService.getUserWorkspaces();

    if (userWorkspaces) {
      userWorkspaces.forEach((space) => {
        space.icon = team;
        space.type = CSS_WORKSPACE_TAG;
      });

      const personalWorkspace = {
        id: PERSONAL_WORKSPACE_ID,
        name: PERSONAL_WORKSPACE_NAME,
        icon: personal,
        type: CSS_WORKSPACE_TAG,
      };

      const isValidPersonalWorkspace =
        await snaptrudeService.checkPersonalWorkspaces();
      if (isValidPersonalWorkspace) {
        userWorkspaces.unshift(personalWorkspace);
      }

      if (userWorkspaces.length) {
        setEntries(userWorkspaces);
      }
    }
  };

  const isUserPro = async () => {
    const isProUser = await snaptrudeService.checkIfProUser();
    setIsProUser(isProUser);
  }

  const getFolders = async () => {
    let folders = await snaptrudeService.getFolders(
      selectedWorkspaceId,
      currentFolderId
    );
   

    if (folders) {
      folders.forEach((f) => {
        f.icon = folder;
        f.type = CSS_FOLDER_TAG;
      });

      const currentFolder = {
        id: currentFolderId,
        name: currentFolderName,
        icon: folder,
        type: CSS_FOLDER_TAG,
      };

      const workspaceIcon = selectedWorkspaceName === PERSONAL_WORKSPACE_NAME ? personal : team;

      const currentWorkspace = {
        id: ROOT_FOLDER_ID,
        name: selectedWorkspaceName,
        icon: workspaceIcon,
        type: CSS_WORKSPACE_TAG,
      };

      const firstEntry = isRootFolderPage ? currentWorkspace : currentFolder;
      folders.unshift(firstEntry);

      if (folders.length) {
        setEntries(folders);
      }
    }
  };

  const isWorkspacesPage = _.isNull(selectedWorkspaceId);
  const isFoldersPage = !isWorkspacesPage;

  const currentFolderId = _.last(foldersArray).id;
  const currentFolderName = _.last(foldersArray).name;

  const parentFolderId = _.nth(foldersArray, -2)?.id ?? ROOT_FOLDER_ID;

  const isRootFolderPage = foldersArray.length === 1;

  const leftButtonText = isWorkspacesPage ? "Cancel" : "Back";
  const rightButtonText = isWorkspacesPage ? "Next" : "Done";

  const leftButtonCallback = isWorkspacesPage
    ? closeApplication
    : isRootFolderPage
    ? goBackToWorkspaces
    : goOneFolderUp;

  const rightButtonCallback = isWorkspacesPage ? chooseWorkspace : onSubmit;
  const entryClickCallback = isWorkspacesPage
    ? onWorkspaceClick
    : onFolderClick;

  const headingText = isWorkspacesPage ? "workspace" : "folder";
  const heading = `Select the ${headingText} to upload to`;

  const initiallySelectedEntryId = isWorkspacesPage
    ? PERSONAL_WORKSPACE_ID
    : ROOT_FOLDER_ID;
  const initiallySelectedEntryName = isWorkspacesPage
    ? PERSONAL_WORKSPACE_NAME
    : "";

  const [isLoading, setIsLoading] = useState(false);
  const [isWorkSpaceLoading, setIsWorkSpaceLoading] = useState(true);
  const [isProUser, setIsProUser] = useState(false);

  const [entries, setEntries] = useState([]);
  const [selectedEntryId, setSelectedEntryId] = useState(
    initiallySelectedEntryId
  );
  const [selectedEntryName, setSelectedEntryName] = useState(
    initiallySelectedEntryName
  );

  useEffect(() => {
    if (isFoldersPage) {
      setIsLoading(true);
      isRootFolderPage
        ? setSelectedEntryId(selectedWorkspaceId)
        : setSelectedEntryId(parentFolderId);
    }else{
      setIsWorkSpaceLoading(true);
    }
    const getEntries = isWorkspacesPage ? getWorkspaces : getFolders;

    getEntries().then(() => {
      setIsLoading(false);
      setIsWorkSpaceLoading(false);
      if (isFoldersPage) {
        setSelectedEntryId(currentFolderId);
      }
    });

    isUserPro().then(() => { });
  }, [selectedWorkspaceId, foldersArray, parentFolderId]);

  if (isWorkSpaceLoading) return <LoadingScreen />;
  if (!entries.length || !isProUser)
    return (
    <UpgradePlan closeApplication = {closeApplication} onSubmitGoToPayment = {onSubmitGoToPayment}/>
    );
  return (
    <Wrapper>
      <div className="content">
        <p>{heading}</p>
        <WorkspacesGrid>
          {entries.map(({ id, name, icon, type }, idx) => {
            const classNameIcon =
              type + (id === selectedEntryId ? "selected" : "");
            const classNameTxt = id === selectedEntryId ? "selected" : "";
            return (
              <Tooltip placement="top" key = {idx} title={name.length > 25 ? name : undefined} color = {colors.primeBlack} >
              <WorkspaceInfo
                key={idx}
                onClick={() => entryClickCallback(id, name)}
              >
                <WorkspaceIcon
                  className={classNameIcon + "-img"}
                  src={icon}
                  alt={"workspace"}
                />
                <WorkspaceTitle className={classNameTxt + "-txt"}>
                  {" "}
                  {name}{" "}
                </WorkspaceTitle>
              </WorkspaceInfo>
              </Tooltip>
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
            title={leftButtonText}
            onPress={leftButtonCallback}
          />
        </div>
        <div className="button-wrapper">
          <Button
            isLoading={isLoading}
            primary={true}
            title={rightButtonText}
            onPress={rightButtonCallback}
          />
        </div>
      </footer>
    </Wrapper>
  );
};

export default Workspace;
