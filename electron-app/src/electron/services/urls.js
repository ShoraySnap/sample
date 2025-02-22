const path = require('path');
const fs = require('fs');
const logger = require("../services/logger");

const urls = (function () {
  const _urls = {
    snaptrudeReactUrl: "https://app.snaptrude.com",
    snaptrudeDjangoUrl: "https://api.snaptrude.com",
  };

  const init = function () {
  
    const appDataPath = path.resolve(process.env.ProgramData, "snaptrude-manager");
    
    const fileName = "urls.json";
    filePath = path.join(appDataPath, fileName);

    if (fs.existsSync(filePath)) {
      const data = _parseDataFile(filePath);
      
      if (data.snaptrudeReactUrl) _urls.snaptrudeReactUrl = data.snaptrudeReactUrl;
      if (data.snaptrudeDjangoUrl) _urls.snaptrudeDjangoUrl = data.snaptrudeDjangoUrl;
    }
  };

  const _parseDataFile = function (filePath) {
    try {
      return JSON.parse(fs.readFileSync(filePath));
    } catch (error) {
      logger.log("Can't read urls file");
      logger.log(error.stack);
      return {};
    }
  };

  const get = function (key) {
    return _urls[key];
  };

  const getAll = function () {
    return _urls;
  };

  return {
    init,
    get,
    getAll,
  };
})();

module.exports = urls;
