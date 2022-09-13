import React from 'react';
import {HashRouter,Link,Route,Routes} from "react-router-dom";
import Home from "./home";
import ChooseProjectLocation from "./chooseProjectLocation";
import {ROUTES} from "./constants";
import Root from "./root";
import Loading from "./loading";

const RoutesContainer = (props)=> {
 
  return (
    <HashRouter>
    <div className="App">
      <Routes>
        <Route path={ROUTES.root} element={<Root/>}/>
        <Route path={ROUTES.home} element={<Home/>}/>
        <Route path={ROUTES.loading} element={<Loading/>}/>
        <Route path={ROUTES.chooseProjectLocation} element={<ChooseProjectLocation/>}/>
      </Routes>
    </div>
    </HashRouter>
  );
}

export default RoutesContainer;