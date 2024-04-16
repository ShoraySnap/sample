const electron = require("electron");
const path = require("path");
const fs = require("fs");
const logger = require("./services/logger");

const userPreferences = (function () {
  let filePath;
  let data;
  const initData = {
    showWarningVisibility: true,
    showWarningReconciliation: true,
  };

  const init = function (options = {}) {
    // Renderer process has to get `app` module via `remote`, whereas the main process can get it directly
    // app.getPath('userData') will return a string of the user's app data directory path.
    const appDataPath = electron.app.getPath("userData");
    filePath = path.join(appDataPath, "userPreferences.json");

    if (fs.existsSync(filePath)) {
      data = _parseDataFile(filePath);
      if (data != {}) {
        return;
      }
    }
    _createEmptyUserPreferences();
  };

  const _parseDataFile = function (filePath) {
    // We'll try/catch it in case the file doesn't exist yet, which will be the case on the first application run.
    // `fs.readFileSync` will return a JSON string which we then parse into a Javascript object
    try {
      return JSON.parse(fs.readFileSync(filePath));
    } catch (error) {
      logger.log(error);
      _createEmptyUserPreferences();
    }
  };

  const _createEmptyUserPreferences = function () {
    data = initData;
    save();
  };

  // This will just return the property on the `data` object
  const get = function (key) {
    return data[key];
  };

  const getData = function () {
    return data;
  };

  // ...and this will set it
  const set = function (key, val) {
    data[key] = val;
  };

  const save = function () {
    // Wait, I thought using the node.js' synchronous APIs was bad form?
    // We're not writing a server so there's not nearly the same IO demand on the process
    // Also if we used an async API and our app was quit before the asynchronous write had a chance to complete,
    // we might lose that data. Note that in a real app, we would try/catch this.
    try {
      fs.writeFileSync(filePath, JSON.stringify(data, null, 2));
    } catch (error) {
      logger.log(error);
    }
  };

  return {
    init,
    get,
    getData,
    set,
    save,
  };
})();

module.exports = userPreferences;
