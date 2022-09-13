import React, {useEffect, useState} from "react";
import {Column, Container, Rows} from "../../components/Styled/Comps";
import login from "../../assets/login.svg";
import upload from "../../assets/upload.svg";
import reconcile from "../../assets/reconcile.svg";
import profile from "../../assets/profile.svg";
import logout from "../../assets/logout.svg";
import sessionData from "../../services/sessionData";
import RowButton from "../../components/RowButton";
import {BUTTONS, ROUTES} from "../constants";
import {useNavigate} from "react-router-dom";
import urls from "../../services/urls";

const openLoginPageInBrowser = () => {
  const logInUrl = urls.get("snaptrudeReactUrl") + "/login?externalAuth=true";
  
  window.electronAPI.openPageInDefaultBrowser(logInUrl);
}

const flushUserData = () => {
  window.electronAPI.flushUserData();
  sessionData.flush();
}

const Home = () => {
  
  const navigate = useNavigate();
  
  const loginButton = {
    id: BUTTONS.login,
    title: "Login to Snaptrude",
    icon: login,
    onClick: openLoginPageInBrowser
  }
  
  const uploadButton = {
    id: BUTTONS.upload,
    title: "Upload to Snaptrude",
    icon: upload,
    onClick: () => {
      window.electronAPI.uploadToSnaptrude();
      // navigate(ROUTES.loading);
      // backend has to initiate the loading page provided everything has worked properly
    }
  }
  
  const reconcileButton = {
    id: BUTTONS.reconcile,
    title: "Reconcile to Revit",
    icon: reconcile,
    onClick: () => {
    
    }
  }
  
  const profileButton = {
    id: BUTTONS.profile,
    title: "Snaptrude User",
    icon: profile,
    onClick: () => {
      setButtons(buttonsInProfileMode);
    }
  }
  
  const switchAccountButton = {
    id: BUTTONS.switch,
    title: "Switch Account",
    icon: profile,
    onClick: () => {
      flushUserData();
      setButtons(buttonsBeforeLogin);
      openLoginPageInBrowser();
    }
  }
  
  const logoutButton = {
    id: BUTTONS.logout,
    title: "Logout",
    icon: logout,
    onClick: () => {
      flushUserData();
      setButtons(buttonsBeforeLogin);
    }
  }
  
  useEffect(() => {
    window.electronAPI.showLoadingPage(async () => {
      navigate(ROUTES.loading);
    });
    
    return window.electronAPI.removeShowLoadingPageHandler;
  }, []);
  
  const updateTemplatesWithUserData = () => {
    const userData = sessionData.getUserData();
    
    profileButton.title = userData.fullname;
  };
  
  const buttonsBeforeLogin = [loginButton, uploadButton, reconcileButton];
  const buttonsAfterLogin = [profileButton, uploadButton, reconcileButton];
  const buttonsInProfileMode = [switchAccountButton, logoutButton];
  
  const userData = sessionData.getUserData();
  // console.log(userData.fullname, "is logged in");
  
  const isLoggedIn = !!userData.accessToken;
  const initState = isLoggedIn ? buttonsAfterLogin : buttonsBeforeLogin;
  
  if (isLoggedIn) updateTemplatesWithUserData();
  
  const [buttons, setButtons] = useState(initState);
  
  useEffect(() => {
    window.electronAPI.handleSuccessfulLogin((event) => {
      updateTemplatesWithUserData();
      
      setButtons(buttonsAfterLogin);
    });
    
    return window.electronAPI.removeSuccessfulLoginHandler;
  }, []);
  
  return (
    <Column>
      <Rows>
        {buttons.map((button, i) => {
          
          let isDisabled = false;
          if (!isLoggedIn){
            if (button.id !== BUTTONS.login) isDisabled = true;
          }
          
          if (button.id === BUTTONS.reconcile) isDisabled = true;
          
          return (
            <RowButton
              title={button.title}
              icon={button.icon}
              onClick={button.onClick}
              isDisabled={isDisabled}
              key={i}
            />
          )
        })}
      </Rows>
    </Column>
  );
}

export default Home;