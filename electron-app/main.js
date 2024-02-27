const { app, BrowserWindow, ipcMain, shell } = require('electron');
const path = require('path');
const isDev = require('electron-is-dev');
const querystring = require("querystring");

const electronCommunicator = require("./src/electron/communicator");
const sessionData = require("./src/electron/sessionData");
const store = require("./src/electron/store");
const logger = require("./src/electron/services/logger");
const urls = require("./src/electron/services/urls");

if (require('electron-squirrel-startup')) return app.quit();
// the app opens a few times and closes during installation
// this prevents the window creation during those times

const CUSTOM_PROTOCOL = 'snaptrude';

let mainWindow;
let deepLinkingUrl;

const parseProtocolArgs = async function (argv){
  
  const _getQueryParamsObject = function (){
    const regex = /snaptrude:\/\/.+[?](.*)/;
    const found = deepLinkingUrl.match(regex);
    
    // found is an array, [0] will be the entire url, [1] will be the query 'group' denoted by (.*)
    
    const queryParamsString = found[1];
    return querystring.decode(queryParamsString);
  }
  
  if (process.platform !== 'darwin') {
    // Find the arg that is our custom protocol url and store it
    deepLinkingUrl = argv.find((arg) => arg.startsWith(CUSTOM_PROTOCOL + '://'));
  }
  
  if (!deepLinkingUrl) return;
  
  // logger.log(argv);
  // logger.log(deepLinkingUrl);
  
  const queryParamsObject = _getQueryParamsObject();
  
  if (deepLinkingUrl.includes("start")){
    // deep link url format-
    // snaptrude://start?name=<model-name>
    
    const revitProjectName = queryParamsObject.name;
    logger.log("Initiated from Revit", revitProjectName);
    
    sessionData.setRevitModelName(revitProjectName);
    electronCommunicator.revitIsWaiting();
  }
  else if (deepLinkingUrl.includes("loginSuccess")){
    // deep link url format-
    // snaptrude://loginSuccess?data=<user-data>
    
    const userData = JSON.parse(queryParamsObject.data);
    store.setAllAndSave(userData);
    
    electronCommunicator.syncSessionData();
    electronCommunicator.updateUIAfterLogin();
    // updates UI
    
    logger.log("Login successful", store.get("fullname"));
    logger.log();
  } else if(deepLinkingUrl.includes("finish")){
    const REACT_URL = urls.get("snaptrudeReactUrl");
    store.set("modelLink", REACT_URL + "/model/" + store.get("floorkey"));
    store.save();
    electronCommunicator.syncSessionData();
    electronCommunicator.revitImportDone();
  }
};

const enableEventListeners = function () {
  if (!app.isDefaultProtocolClient(CUSTOM_PROTOCOL)) {
    if (isDev && process.platform === 'win32') {
      // Set the path of electron.exe and your app.
      // These two additional parameters are only available on windows.
      // Setting this is required to get this working in dev mode.
      app.setAsDefaultProtocolClient(CUSTOM_PROTOCOL, process.execPath, [
        path.resolve(process.argv[1])
      ]);
    } else {
      app.setAsDefaultProtocolClient(CUSTOM_PROTOCOL);
    }
  }
  
  // Force single application instance
  const gotTheLock = app.requestSingleInstanceLock();
  
  if (!gotTheLock) {
    app.quit();
    return;
  } else {
    app.on('second-instance', (e, argv) => {
      
      if (mainWindow) {
        if (mainWindow.isMinimized()) mainWindow.restore();
        mainWindow.focus();
      }
      
      parseProtocolArgs(argv);
      
    });
  }
  
  app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit();
    // app.removeAsDefaultProtocolClient(CUSTOM_PROTOCOL);
  });
  
  app.on('before-quit', () => {
    // in case manager is closed after opening from revit
    electronCommunicator.sendPipeCommandToStopWaiting();
  });
  
  app.whenReady().then(async () => {
    ipcMain.on('openPageInDefaultBrowser', electronCommunicator.openPageInDefaultBrowser);
    ipcMain.on('flushUserData', () => {
      logger.log("User logged out", store.get("fullname"));
      store.flush();
    });
    ipcMain.on('updateUserData', (event, [data]) => store.setAllAndSave(data));
    ipcMain.on('uploadToSnaptrude',(event, [teamId, folderId]) => electronCommunicator.uploadToSnaptrude(teamId, folderId));
    ipcMain.on('importFromSnaptrude', electronCommunicator.importFromSnaptrude);
    ipcMain.on('log', (event, [messages]) => logger.log(...messages));
    ipcMain.on('operationSucceeded', electronCommunicator.operationSucceeded);
    ipcMain.on('operationFailed', electronCommunicator.operationFailed);
    ipcMain.on('closeApplication', electronCommunicator.closeApplication);
    ipcMain.on('openDevtools', electronCommunicator.openDevtools);
    ipcMain.on('showLogs', logger.showLogs);
    
    store.init();
    urls.init();
    await createWindow();
    
    electronCommunicator.init(mainWindow);
    logger.init(mainWindow);
    
    await parseProtocolArgs(process.argv);
    
    electronCommunicator.syncSessionData();
    electronCommunicator.setUrls();
    electronCommunicator.goHome();
    
    logger.log("App initialized");
  });
  
}

const createWindow = async () => {
  mainWindow = new BrowserWindow({
    title: 'Snaptrude Manager',
    width: 585,
    height: 344,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      // enableRemoteModule: true,
      // nodeIntegration: true,
      // contextIsolation: false,
    },
    resizable: false,
  });
  
  // remote module not recommended by electron anymore
  // https://www.electronjs.org/docs/latest/breaking-changes#default-changed-enableremotemodule-defaults-to-false
  // https://nornagon.medium.com/electrons-remote-module-considered-harmful-70d69500f31
  
  // to use it, multiple security features like nodeIntegration and contextIsolation have to be overridden
  // Just use IPC https://www.electronjs.org/docs/latest/tutorial/ipc
  
  /*
  Because the main and renderer processes have different responsibilities in Electron's process model,
  IPC is the only way to perform many common tasks,
  such as calling a native API from your UI or triggering changes in your web contents from native menus.
  
   */
  
  mainWindow.removeMenu();
  
  const devUrl = 'http://localhost:3005';
  const installedUrl = `file://${path.join(__dirname, './build/index.html')}`
  
  const homeUrl = isDev ? devUrl : installedUrl;
  // const homeUrl = installedUrl;
  
  await mainWindow.loadURL(homeUrl);
  // if (isDev) mainWindow.webContents.openDevTools({ mode: 'detach' });
  
  // Emitted when the window is closed.
  mainWindow.on('closed', function() {
    // Dereference the window object, usually you would store windows
    // in an array if your app supports multi windows, this is the time
    // when you should delete the corresponding element.
    mainWindow = null;
  });
}

enableEventListeners();
