import React from "react";
import { HashRouter, Route, Routes } from "react-router-dom";
import Home from "./home";
import ChooseProjectLocation from "./chooseProjectLocation";
import { ROUTES } from "./constants";
import Root from "./root";
import Loading from "./loading";
import ProjectTypeSelection from "./projectTypeSelection";
import EnterModelLink from "./enterModelLink";
import WarningReconciliation from "./warning/WarningReconciliation";
import WarningVisibility from "./warning/WarningVisibility";

const RoutesContainer = (props) => {
  return (
    <HashRouter>
      <div className="App">
        <Routes>
          <Route path={ROUTES.root} element={<Root />} />
          <Route path={ROUTES.home} element={<Home />} />
          <Route path={ROUTES.loading} element={<Loading />} />
          <Route
            path={ROUTES.projectTypeSelection}
            element={<ProjectTypeSelection />}
          />
          <Route
            path={ROUTES.warningVisibility}
            element={<WarningVisibility />}
          />
          <Route path={ROUTES.enterModelLink} element={<EnterModelLink />} />
          <Route
            path={ROUTES.warningReconciliation}
            element={<WarningReconciliation />}
          />
          <Route
            path={ROUTES.chooseProjectLocation}
            element={<ChooseProjectLocation />}
          />
        </Routes>
      </div>
    </HashRouter>
  );
};

export default RoutesContainer;
