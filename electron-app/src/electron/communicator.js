const net = require("net");
const {shell, app} = require("electron");
const speckleService = require("./services/speckle.service");
const store = require("./store");
const logger = require("../electron/services/logger");
const fs = require("fs");
const path = require("path");
const electron = require("electron");
const urls = require("./services/urls");
const sessionData = require("../electron/sessionData");

const electronCommunicator = (function (){
  
  let mainWindow;
  
  const REVIT_PIPE_MSG_BEGIN_IMPORT = "beginImport"; // 11 characters
  const REVIT_PIPE_MSG_BEGIN_EXPORT = "beginExport"; // 11 characters
  const REVIT_PIPE_MSG_STOP = "stopWaiting"; // 11 characters
  const REVIT_WAIT_THRESHOLD = 60 * 1e3;
  
  const PIPE_NAME = 'snaptrudeRevitPipe';
  const PIPE_PATH = '\\\\.\\pipe\\'; // The format is \\.\pipe\<name>
  
  let isRevitWaiting = false;
  let timeoutId;
  
  const init = function (window){
    mainWindow = window;
  }
  
  const openPageInDefaultBrowser = function (event, [url, flush]){
    // if (flush) store.flush();
    shell.openExternal(url).catch(e => {
      logger.log("Cannot open url in the default browser", url);
      logger.log(e.stack)
    });
  };

  const sendPipeCommandForImport = function() {
    if (!isRevitWaiting) return;
    
    try {
      const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
        logger.log('Connected to Revit pipe server!', 'Sending command to import from Snaptrude');
        client.write(REVIT_PIPE_MSG_BEGIN_IMPORT);
        
        if (timeoutId) clearTimeout(timeoutId);
        isRevitWaiting = false;
        mainWindow.close();
      });
      
      client.on('data', (data) => {
        logger.log(data.toString());
      });
      
      client.on('end', () => {
        logger.log('Disconnected from Revit pipe server');
      });
    }
    catch (e) {
      logger.log("No pipe server");
    }
    
  }
  
  const sendPipeCommandForExport = function (){
    if (!isRevitWaiting) return;
    
    try {
      const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
        logger.log('Connected to Revit pipe server!', 'Sending command to export to Snaptrude');
        client.write(REVIT_PIPE_MSG_BEGIN_EXPORT);
        
        if (timeoutId) clearTimeout(timeoutId);
        isRevitWaiting = false;
      });
      
      client.on('data', (data) => {
        logger.log(data.toString());
      });
      
      client.on('end', () => {
        logger.log('Disconnected from Revit pipe server');
      });
    }
    catch (e) {
      logger.log("No pipe server");
    }
    
  }
  
  const sendPipeCommandToStopWaiting = function (){
    if (!isRevitWaiting) return;
    
    try {
      const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
        logger.log('Connected to Revit pipe server!', 'Sending command to stop waiting');
        client.write(REVIT_PIPE_MSG_STOP);
        
        isRevitWaiting = false;
      });
      
      client.on('data', (data) => {
        logger.log(data.toString());
      });
      
      client.on('end', () => {
        logger.log('Disconnected from Revit pipe server');
      });
    }
    catch (e) {
      logger.log("No pipe server");
    }
    
  }
  
  const revitIsWaiting = function () {
    isRevitWaiting = true;
    timeoutId = setTimeout(sendPipeCommandToStopWaiting, REVIT_WAIT_THRESHOLD);
  }
  
  const sendStreamIdToDynamoForExport = function (streamId){
    const PIPE_NAME = 'snaptrudeDynamoPipe';
    const PIPE_PATH = '\\\\.\\pipe\\'; // The format is \\.\pipe\<name>
    
    let server;
    
    try {
      
      server = net.createServer();
      
      // server.listen(path.join(PIPE_PATH, process.cwd(), PIPE_NAME));
      server.listen(PIPE_PATH + PIPE_NAME);
      
      server.on('data', (data) => {
        logger.log(data.toString());
      });
      
      server.on('connection', (socket) => {
        logger.log('someone connected to server');
        
        socket.write(streamId);
        server.close();
      });
      
      server.on('end', () => {
        logger.log('closed server');
      });
    }
    catch (e) {
      logger.log("No pipe server");
      logger.log(e);
    }
    
  }

  const importFromSnaptrude = async function () {
    if (!isRevitWaiting) {
      logger.log("Upload clicked but Revit is not waiting for a command");
      return;
    }

    sendPipeCommandForImport();
  }
  
  const uploadToSnaptrude = async function () {
    if (!isRevitWaiting) {
      logger.log("Upload clicked but Revit is not waiting for a command");
      return;
    }
    
    logger.log("Uploading to Snaptrude");
    const streamId = await electronCommunicator.generateStreamID();
    
    if (streamId) {
      logger.log("Generated stream ID", streamId);
      syncSessionData();
      sendPipeCommandForExport();
      updateUIShowLoadingPage();
      await startPollingForStreamUploadCompletion();
    }
  }
  
  const generateStreamID = async function (){
    const streamId = await speckleService.generateStreamId();
    // const streamId = "02adcc3b10";
    
    // write to a file that'll be used by the .dyn script
    // sendStreamIdToDynamoForExport(streamId);
    
    if (streamId) {
      store.set('streamId', streamId);
      store.set('revitProjectName', sessionData.getRevitModelName());
      store.save();
    }
    
    return streamId;
  }
  
  const ensureDirectoryExistence = function (filePath) {
    const dirname = path.dirname(filePath);
    if (fs.existsSync(dirname)) {
      return true;
    }
    logger.log("Creating directory for Speckle/Accounts/account.json")
    fs.mkdirSync(dirname);
  }
  
  const writeAccountInfoForSpeckleConnector = function (){
    const data = {
      "token": store.get('speckleApiToken'),
      "serverInfo": {
        "name": "Speckle",
        "company": "Speckle",
        "url": urls.get("speckleUrl")
      },
      "userInfo": {
        "id": "userid",
        "name": "Speckle User",
        "email": "speckle@user.com"
      }
    }
    
    const appDataSnaptrudeManagerPath = electron.app.getPath('userData');
    const fileName = "Speckle/Accounts/account.json";
    
    const filePath = path.join(appDataSnaptrudeManagerPath, '../', fileName);
    
    try {
      ensureDirectoryExistence(filePath);
      fs.writeFileSync(filePath, JSON.stringify(data, null, 2));
    } catch(error) {
      logger.log(error);
    }
  }
  
  const updateUIAfterLogin = function (){
    mainWindow.webContents.send('handleSuccessfulLogin');
  }
  
  const updateUIAfterSpeckleUpload = function (){
    mainWindow.webContents.send('handleSuccessfulSpeckleUpload');
  }
  
  const updateUIShowLoadingPage = function (){
    mainWindow.webContents.send('showLoadingPage');
  }
  
  const startPollingForStreamUploadCompletion = async function (){
    const uploadDone = await speckleService.startPolling();
    
    if (uploadDone) {
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.focus();
      updateUIAfterSpeckleUpload();
    }
    else {
      // updateUIErrorPage();
    }
  }
  
  const operationSucceeded = function () {
    logger.log("Operation succeeded");
    logger.addSeparator();
    
    store.unset("streamId");
    store.unset("revitProjectName");
    store.save();
    
    // syncSessionData();
    app.quit();
  }
  
  const operationFailed = function () {
    logger.log("Operation failed");
    logger.addSeparator();
    
    store.unset("streamId");
    store.unset("revitProjectName");
    store.save();
    
    // syncSessionData();
    app.quit();
  }

  const closeApplication = function () {
    logger.log("Upload cancelled");
    app.quit();
  }
  
  const syncSessionData = function (data = store.getData()){
    mainWindow.webContents.send('syncSessionData', data);
  }
  
  const setUrls = function (){
    mainWindow.webContents.send('setUrls', urls.getAll());
  }
  
  const goHome = function (){
    mainWindow.webContents.send('goHome');
  }
  
  const openDevtools = function () {
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  }
  
  return {
    sendPipeCommandForExport,
    sendPipeCommandToStopWaiting,
    init,
    openPageInDefaultBrowser,
    generateStreamID,
    startPollingForStreamUploadCompletion,
    uploadToSnaptrude,
    importFromSnaptrude,
    operationSucceeded,
    operationFailed,
    revitIsWaiting,
    writeAccountInfoForSpeckleConnector,
    setUrls,
    openDevtools,
    
    updateUIAfterLogin,
    updateUIShowLoadingPage,
    syncSessionData,
    goHome,
    closeApplication,
  }
  
})();

module.exports = electronCommunicator;