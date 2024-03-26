import React from "react";
import styled from "styled-components";
import {colors} from "../../themes/constant";
import {useNavigate} from "react-router-dom";
import {ROUTES} from "../constants";
import Button from "../../components/Button";

const Wrapper = styled.div`
  padding: 0em 9em 0em 9em;
  .content {
    display: flex;
    flex-direction: column;
    padding: 5em 1em 5em 1em;
  }
`;


const ProjectSelection = (props) => {
  const navigate = useNavigate();
  return (
    <Wrapper>
      <div className="content">
          <Button
            weight={500}
            hover={true}
            customButtonStyle={{
              backgroundColor: colors.fullWhite,
              color: colors.black,
              border: '1px solid black'
            }}
            title={"Create New Project"}
            onPress={()=>{navigate(ROUTES.chooseProjectLocation)}}
          />
          <p>{"or"}</p>
          <Button
            className="button"
            weight={500}
            hover={true}
            customButtonStyle={{
              backgroundColor: colors.fullWhite,
              color: colors.black,
              border: '1px solid black',
            }}
            title={"Upload Existing Project"}
            onPress={()=>{navigate(ROUTES.enterModelLink)}}
          />
        </div>
    </Wrapper>
  );
}

export default ProjectSelection;