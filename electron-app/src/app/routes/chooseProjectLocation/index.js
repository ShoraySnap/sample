import React, { useState } from "react";
import styled from "styled-components";
import Workspace from "./Workspace";
import { colors } from "../../themes/constant";
import { ROOT_FOLDER_ID } from "../constants";

const Wrapper = styled.div`
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
    justify-content: space-around;
  }
  .button-wrapper {
    z-index: 5;
  }
  .button-wrapper button {
    min-width: 12em;
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

const ChooseProjectLocation = (props) => {
  const rootFolder = {
    id: ROOT_FOLDER_ID,
    name: "",
  };

  const [selectedWorkspaceId, setSelectedWorkspaceId] = useState(null);
  const [selectedWorkspaceName, setSelectedWorkspaceName] = useState("");
  const [foldersArray, setFoldersArray] = useState([rootFolder]);

  return (
    <Wrapper>
      <Workspace
        selectedWorkspaceId={selectedWorkspaceId}
        setSelectedWorkspaceId={setSelectedWorkspaceId}
        selectedWorkspaceName={selectedWorkspaceName}
        setSelectedWorkspaceName={setSelectedWorkspaceName}
        foldersArray={foldersArray}
        setFoldersArray={setFoldersArray}
      />
    </Wrapper>
  );
};

export default ChooseProjectLocation;
