import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import App from "./App";
import reportWebVitals from "./reportWebVitals";
import sessionData from "./app/services/sessionData";
import urls from "./app/services/urls";
import * as mousetrap from "mousetrap";
import userPreferences from "./app/services/userPreferences";

const root = ReactDOM.createRoot(document.getElementById("root"));
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();

/*window.electronAPI = {
  handleSuccessfulLogin: () => {},
  syncSessionData: () => {},
  goHome: () => {},
  handleSuccessfulUpload: () => {},
  log: () => {},
};*/

window.electronAPI.syncSessionData((event, data) => {
  sessionData.setUserData(data);
});

window.electronAPI.syncUserPreferences((event, data) => {
  userPreferences.setData(data);
});

window.electronAPI.setUrls((event, data) => {
  urls.init(data);
});

mousetrap.bind("o p e n s e s a m e", function () {
  window.electronAPI.openDevtools();
});

mousetrap.bind("l o g s", function () {
  window.electronAPI.showLogs();
});

window.sessionData = sessionData;
