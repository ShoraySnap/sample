import React from "react";
import { ROUTES } from "../constants";
import { useNavigate } from "react-router-dom";
import _ from "lodash";
import userPreferences from "../../services/userPreferences";
import { WarningTemplate } from "./WarningTemplate";

const WarningReconciliation = ({}) => {
  let showWarningAgain = true;
  const navigate = useNavigate();

  const onBack = () => {
    navigate(ROUTES.projectTypeSelection);
  };

  const onSubmit = async () => {
    if (showWarningAgain == false) {
      window.electronAPI.updateUserPreferences(
        "showWarningReconciliation",
        showWarningAgain
      );
      userPreferences.set("showWarningReconciliation", showWarningAgain);
    }
    navigate(ROUTES.enterModelLink);
  };

  const leftButtonCallback = onBack;
  const rightButtonCallback = onSubmit;

  const onCheckbox = (e) => {
    showWarningAgain = !e.target.checked;
  };

  return (
    <WarningTemplate
      note="The Revit model will directly export to Snaptrude without being reconciled with the existing model."
      backButton={leftButtonCallback}
      nextButton={rightButtonCallback}
      onCheckbox={onCheckbox}
    ></WarningTemplate>
  );
};

export default WarningReconciliation;
