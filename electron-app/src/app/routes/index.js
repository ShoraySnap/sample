import React from 'react';
import {HashRouter,Route,Routes} from "react-router-dom";
import Home from "./home";
import ChooseProjectLocation from "./chooseProjectLocation";
import {ROUTES} from "./constants";
import Root from "./root";
import Loading from "./loading";
import ProjectSelection from "./projectSelection";
import EnterModelLink from "./enterModelLink";

const RoutesContainer = (props)=> {
 
  return (
    <HashRouter>
    <div className="App">
      <Routes>
        <Route path={ROUTES.root} element={<Root/>}/>
        <Route path={ROUTES.home} element={<Home/>}/>
        <Route path={ROUTES.loading} element={<Loading/>}/>
        <Route path={ROUTES.projectSelection} element={<ProjectSelection/>}/>
        <Route path={ROUTES.enterModelLink} element={<EnterModelLink/>}/>
        <Route path={ROUTES.chooseProjectLocation} element={<ChooseProjectLocation/>}/>
      </Routes>
    </div>
    </HashRouter>
  );
}

export default RoutesContainer;