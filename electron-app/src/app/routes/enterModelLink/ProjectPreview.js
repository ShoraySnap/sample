import React from "react";
import styled from "styled-components";
import { colors } from "../../themes/constant";

const ProjectPreviewWrapper = styled.div`
  width: 100%;
  height: 4em;
  box-sizing: border-box;
  border: 1px solid ${colors.Neutral[200]};
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
    color: ${colors.Neutral[600]};
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
            backgroundColor: colors.Neutral[200],
          }}
        ></div>
        <p className="main-items">{projectName}</p>
      </div>
    </ProjectPreviewWrapper>
  );
};

export default ProjectPreview;
