import React from "react";
import { ROUTES } from "../constants";
import { useNavigate } from "react-router-dom";
import sessionData from "../../services/sessionData";
import userPreferences from "../../services/userPreferences";
import { WarningTemplate } from "./WarningTemplate";

const WarningVisibility = ({}) => {
  let showWarningAgain = true;
  const navigate = useNavigate();

  const onBack = () => {
    navigate(ROUTES.home);
  };

  const onSubmit = async () => {
    if (showWarningAgain == false) {
      window.electronAPI.updateUserPreferences(
        "showWarningVisibility",
        showWarningAgain
      );
      userPreferences.set("showWarningVisibility", showWarningAgain);
    }

    fileType == "rfa"
      ? navigate(ROUTES.projectTypeSelection)
      : navigate(ROUTES.chooseProjectLocation);
  };

  const fileType = sessionData.getUserData().fileType;
  const leftButtonCallback = onBack;
  const rightButtonCallback = onSubmit;

  const onCheckbox = (e) => {
    showWarningAgain = !e.target.checked;
  };

  return (
    <WarningTemplate
      note="All the visible parts of the model will export to Snaptrude. Hide or remove any elements that you don’t wish to export before proceeding."
      backButton={leftButtonCallback}
      nextButton={rightButtonCallback}
      onCheckbox={onCheckbox}
    ></WarningTemplate>
  );
};

export default WarningVisibility;
