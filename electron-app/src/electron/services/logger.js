const path = require("path");
const fs = require("fs");
const os = require("os");
const _ = require("lodash");
const { shell, app } = require("electron");

const logger = (() => {
  let mainWindow;
  let filePath;
  let fileExists;

  const init = function (window) {
    mainWindow = window;

    const appDataPath = app.getPath("userData");

    const fileName = "app.log";
    filePath = path.join(appDataPath, fileName);

    if (fs.existsSync(filePath)) {
      fileExists = true;

      const stats = fs.statSync(filePath);
      const fileSizeInBytes = stats.size;
      const fileSizeInMegabytes = fileSizeInBytes / (1024 * 1024);

      const threshold = 2; //2 MB

      if (fileSizeInMegabytes > threshold) {
        _createEmptyFile();
      }
    } else _createEmptyFile();
  };

  const _createEmptyFile = function () {
    try {
      fs.writeFileSync(filePath, "Log start");
      fileExists = true;
    } catch (error) {
      fileExists = false;
    }
  };

  const _writeToFile = function (message) {
    if (!fileExists) return;

    fs.appendFile(filePath, message, (err) => {
      if (err) {
        console.log(err);
      }
    });
  };

  const _sanitize = function (message) {
    if (_.isObject(message)) message = JSON.stringify(message, null, 2);
    return new Date().toString() + message + os.EOL;
  };

  const log = function (...messages) {
    try {
      const message = messages.length === 1 ? messages[0] : messages.join(" ");

      console.log(message);
      if (mainWindow && mainWindow.webContents) {
        mainWindow.webContents.executeJavaScript(`console.log("${message}")`);
      }

      _writeToFile(_sanitize(message));
    } catch (e) {
      // sometimes log is called when electron is shutting down
      // getting object has been destroyed error
    }
  };

  const addSeparator = function () {
    _writeToFile(os.EOL);
  };

  const showLogs = function () {
    shell.openPath(filePath).then((e) => {
      if (e !== "") log("Cannot open log file");
    });
  };

  return {
    init,
    log,
    addSeparator,
    showLogs,
  };
})();

module.exports = logger;
