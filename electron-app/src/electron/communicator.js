const net = require("net");
const { shell, app } = require("electron");
const store = require("./store");
const logger = require("../electron/services/logger");
const fs = require("fs");
const path = require("path");
const electron = require("electron");
const urls = require("./services/urls");
const sessionData = require("../electron/sessionData");
const snaptrudeService = require("./services/snaptrude.service");
const userPreferences = require("./UserPreferences");

const electronCommunicator = (function () {
  let mainWindow;

  const REVIT_PIPE_MSG_BEGIN_IMPORT = "beginImport"; // 11 characters
  const REVIT_PIPE_MSG_BEGIN_EXPORT = "beginExport"; // 11 characters
  const REVIT_PIPE_MSG_STOP = "stopWaiting"; // 11 characters
  const REVIT_WAIT_THRESHOLD = 120 * 1e3;
  const REVIT_PIPE_MEG_FINISH_IMPORT = "done-Import"; // 11 characters

  const PIPE_NAME = "snaptrudeRevitPipe";
  const PIPE_PATH = "\\\\.\\pipe\\"; // The format is \\.\pipe\<name>

  let isRevitWaiting = false;
  let timeoutId;

  const init = function (window) {
    mainWindow = window;
  };

  const openPageInDefaultBrowser = function (event, [url, flush]) {
    // if (flush) store.flush();
    shell.openExternal(url).catch((e) => {
      logger.log("Cannot open url in the default browser", url);
      logger.log(e.stack);
    });
  };

  const sendPipeCommandForImport = function () {
    if (!isRevitWaiting) return;

    try {
      const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
        logger.log(
          "Connected to Revit pipe server!",
          "Sending command to import from Snaptrude"
        );
        client.write(REVIT_PIPE_MSG_BEGIN_IMPORT);

        if (timeoutId) clearTimeout(timeoutId);
        isRevitWaiting = false;
        mainWindow.close();
      });

      client.on("data", (data) => {
        logger.log(data.toString());
      });

      client.on("end", () => {
        logger.log("Disconnected from Revit pipe server");
      });
    } catch (e) {
      logger.log("No pipe server");
    }
  };

  const sendPipeCommandForExport = function () {
    if (!isRevitWaiting) return;

    try {
      const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
        logger.log(
          "Connected to Revit pipe server!",
          "Sending command to export to Snaptrude"
        );
        client.write(REVIT_PIPE_MSG_BEGIN_EXPORT);

        if (timeoutId) clearTimeout(timeoutId);
        isRevitWaiting = false;
      });

      client.on("data", (data) => {
        logger.log(data.toString());
      });

      client.on("end", () => {
        logger.log("Disconnected from Revit pipe server");
      });
    } catch (e) {
      logger.log("No pipe server");
    }
  };

  const sendPipeCommandToStopWaiting = function () {
    if (!isRevitWaiting) return;

    try {
      const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
        logger.log(
          "Connected to Revit pipe server!",
          "Sending command to stop waiting"
        );
        client.write(REVIT_PIPE_MSG_STOP);

        isRevitWaiting = false;
      });

      client.on("data", (data) => {
        logger.log(data.toString());
      });

      client.on("end", () => {
        logger.log("Disconnected from Revit pipe server");
      });
    } catch (e) {
      logger.log("No pipe server");
    }
  };

  const sendPipeCommandForDoneImport = function () {
    if (!isRevitWaiting) return;

    try {
      const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
        logger.log(
          "Connected to Revit pipe server!",
          "Sending command for done import"
        );
        client.write(REVIT_PIPE_MEG_FINISH_IMPORT);

        if (timeoutId) clearTimeout(timeoutId);
        isRevitWaiting = false;
      });

      client.on("data", (data) => {
        logger.log(data.toString());
      });

      client.on("end", () => {
        logger.log("Disconnected from Revit pipe server");
      });
    } catch (e) {
      logger.log("No pipe server");
    }
  };

  const revitIsWaiting = function () {
    isRevitWaiting = true;
    timeoutId = setTimeout(sendPipeCommandToStopWaiting, REVIT_WAIT_THRESHOLD);
  };

  const importFromSnaptrude = async function () {
    if (!isRevitWaiting) {
      logger.log("Upload clicked but Revit is not waiting for a command");
      return;
    }

    sendPipeCommandForImport();
  };

  const uploadToExistingProject = async function (url) {
    // taufiqul
  };

  const uploadRFAToExistingProject = async function (url) {
    let floorKey = url.endsWith("/")
      ? url.slice(-7).slice(0, 6)
      : url.slice(-6);

    if (!isRevitWaiting) {
      logger.log("Upload clicked but Revit is not waiting for a command");
      return;
    }

    logger.log("Uploading to Snaptrude");

    const snaptrudeProject = floorKey;

    const revitImportState = await snaptrudeService.flagRevitImportState(
      snaptrudeProject,
      "RFA"
    );
    if (!revitImportState) {
      logger.log("Failed to flag revit import state!");
      return;
    }
    store.set("revitImportState", revitImportState);
    store.set("floorkey", snaptrudeProject);

    store.save();

    logger.log("Generated model", snaptrudeProject);
    syncSessionData();
    syncUserPreferences();
    sendPipeCommandForExport();
    updateUIShowLoadingPage();
  }

  const uploadRFAToSnaptrude = async function (teamId, folderId) {
    if (!isRevitWaiting) {
      logger.log("Upload clicked but Revit is not waiting for a command");
      return;
    }

    logger.log("Uploading to Snaptrude");

    const snaptrudeProject = await snaptrudeService.createProject(
      teamId,
      folderId
    );
    if (!snaptrudeProject) {
      logger.log("Error creating Snaptrude project");
      return;
    }

    const revitImportState = await snaptrudeService.flagRevitImportState(
      snaptrudeProject,
      "RFA"
    );
    if (!revitImportState) {
      logger.log("Failed to flag revit import state!");
      return;
    }
    store.set("revitImportState", revitImportState);
    store.set("floorkey", snaptrudeProject);

    store.save();

    logger.log("Generated model", snaptrudeProject);
    syncSessionData();
    syncUserPreferences();
    sendPipeCommandForExport();
    updateUIShowLoadingPage();
  }

  const uploadToSnaptrude = async function (teamId, folderId) {
    if (!isRevitWaiting) {
      logger.log("Upload clicked but Revit is not waiting for a command");
      return;
    }

    logger.log("Uploading to Snaptrude");

    // TODO: ADD SOME FUNCTIONALITY TO UPLOAD TO EXISTING PROJECT
    const snaptrudeProject = await snaptrudeService.createProject(
      teamId,
      folderId
    );
    if (!snaptrudeProject) {
      logger.log("Error creating Snaptrude project");
      return;
    }

    const revitImportState = await snaptrudeService.flagRevitImportState(
      snaptrudeProject,
      "NEW"
    );
    if (!revitImportState) {
      logger.log("Failed to flag revit import state!");
      return;
    }
    store.set("revitImportState", revitImportState);
    store.set("floorkey", snaptrudeProject);

    store.save();

    logger.log("Generated model", snaptrudeProject);
    syncSessionData();
    syncUserPreferences();
    sendPipeCommandForExport();
    updateUIShowLoadingPage();
  };

  // store.set("revitProjectName", sessionData.getRevitModelName());

  const updateUIAfterLogin = function () {
    mainWindow.webContents.send("handleSuccessfulLogin");
  };

  const updateUIAfterS3Upload = function () {
    mainWindow.webContents.send("handleSuccessfulUpload");
  };

  const updateUIShowLoadingPage = function () {
    mainWindow.webContents.send("showLoadingPage");
  };

  const revitImportDone = async function () {
    if (mainWindow.isMinimized()) mainWindow.restore();
    mainWindow.focus();
    updateUIAfterS3Upload();
  };

  const operationSucceeded = function () {
    logger.log("Operation succeeded");
    logger.addSeparator();

    store.unset("streamId");
    store.unset("revitProjectName");
    store.save();

    // syncSessionData();
    app.quit();
  };

  const operationFailed = function () {
    logger.log("Operation failed");
    logger.addSeparator();

    store.unset("streamId");
    store.unset("revitProjectName");
    store.save();

    // syncSessionData();
    app.quit();
  };

  const closeApplication = function () {
    logger.log("Upload cancelled");
    app.quit();
  };

  const syncSessionData = function (data = store.getData()) {
    mainWindow.webContents.send("syncSessionData", data);
  };

  const syncUserPreferences = function (data = userPreferences.getData()) {
    mainWindow.webContents.send("syncUserPreferences", data);
  };

  const setUrls = function () {
    mainWindow.webContents.send("setUrls", urls.getAll());
  };

  const goHome = function () {
    mainWindow.webContents.send("goHome");
  };

  const openDevtools = function () {
    mainWindow.webContents.openDevTools({ mode: "detach" });
  };

  return {
    sendPipeCommandForExport,
    sendPipeCommandToStopWaiting,
    sendPipeCommandForDoneImport,
    init,
    openPageInDefaultBrowser,
    revitImportDone,
    uploadToSnaptrude,
    uploadToExistingProject,
    uploadRFAToSnaptrude,
    uploadRFAToExistingProject,
    importFromSnaptrude,
    operationSucceeded,
    operationFailed,
    revitIsWaiting,
    setUrls,
    openDevtools,
    updateUIAfterLogin,
    updateUIShowLoadingPage,
    syncSessionData,
    syncUserPreferences,
    // updateUserPreferences,
    goHome,
    closeApplication,
  };
})();

module.exports = electronCommunicator;
