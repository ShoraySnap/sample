import React from "react";
import styled from "styled-components";

const ProjectPreviewWrapper = styled.div`
  width: 100%;
  height: 4em;
  box-sizing: border-box;
  border: 2px solid #e8e9ed;
  border-radius: 0.5em;
  overflow: hidden;

  .project-preview {
    display: flex;
    flex-direction: row;
    align-items: center;
    flex-wrap: nowrap;
    gap: 1em;
    height: 100%;
    padding-left: 0.5em;
  }
  .main-items {
    color: #767b93;
  }

  .square-image {
    background-repeat: no-repeat;
    background-size: cover;
    background-position: center;
    width: 3em;
    height: 3em;
    border-radius: 0.5em;
  }
`;

const ProjectPreview = ({ imageURL, projectName }) => {
  return (
    <ProjectPreviewWrapper>
      <div className="project-preview">
        <div
          className="square-image"
          style={{
            backgroundImage: `url(${imageURL})`,
            backgroundColor: "#e8e9ed",
          }}
        ></div>
        <p className="main-items">{projectName}</p>
      </div>
    </ProjectPreviewWrapper>
  );
};

export default ProjectPreview;
