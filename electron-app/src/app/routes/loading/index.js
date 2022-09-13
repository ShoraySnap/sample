import {colors} from "../../themes/constant";
import {useEffect, useRef, useState} from "react";
import ProgressBar from "../../components/ProgressBar";
import styled from "styled-components";
import {useNavigate} from "react-router-dom";
import {ROUTES} from "../constants";

const Container = styled.div`
  display: flex;
  flex-direction: column;
`;

const Text = styled.p`
  margin: 120px;
  font-size: 15px;
`;

const ModalFooter = styled.div`
  position: fixed;
  left: 0;
  bottom: 0;
  right: 0;
  
  display: flex;
  justify-content: space-around;
  padding: 0.25em 0em;
  width: 100%;
  z-index: -2;
  border-top: 0.0625em solid ${colors.veryLightGrey};
  margin-top: 20px;
`;

const progressTexts = [
  "Opening Dynamo",
  "Uploading model",
  "Converting data",
  "Recreating objects",
  "Getting your 3D model ready",
];

function useInterval(callback, delay) {
  const savedCallback = useRef();

  // Remember the latest callback.
  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  // Set up the interval.
  useEffect(() => {
    function tick() {
      savedCallback.current();
    }
    if (delay !== null) {
      let id = setInterval(tick, delay);
      return () => clearInterval(id);
    }
  }, [delay]);
}

let intervalId;

const Loading = (props) => {
  
  const navigate = useNavigate();
  
  useEffect(() => {
    window.electronAPI.handleSuccessfulSpeckleUpload(async () => {
      // navigate(ROUTES.chooseProjectLocation,{ state: {workspaces: workspaces}});
      navigate(ROUTES.chooseProjectLocation);
    });
    
    return window.electronAPI.removeSuccessfulSpeckleUploadHandler;
  }, []);
  
  
  const [progress, setProgress] = useState(1);
  const [progressText, setProgressText] = useState(progressTexts[0]);
  
  const expectedRunTime = 5 * 60 * 1e3; // 5 mins
  const delay = expectedRunTime / 100;
  
  useInterval(() => {
    if (progress < 90) {
      setProgress(progress + 1);
      
      if (progress > 0 && progress <= 10) setProgressText(progressTexts[0]);
      else if (progress > 10 && progress <= 25) setProgressText(progressTexts[1]);
      else if (progress > 25 && progress <= 50) setProgressText(progressTexts[2]);
      else if (progress > 50 && progress <= 75) setProgressText(progressTexts[3]);
      else if (progress > 75 && progress <= 100) setProgressText(progressTexts[4]);
    }
    else {
      // will be stuck at 90
      // cleared when handleSuccessfulSpeckleUpload is called
    }
    
  }, delay);
  
  return (
    <Container>
      <Text>
        Your Revit model is being uploaded to Snaptrude
      </Text>
      <ModalFooter>
        <ProgressBar bgColor={colors.red} completed={progress} text={progressText}/>
      </ModalFooter>
    </Container>
  )
  
};

export default Loading;