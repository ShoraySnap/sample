/*
Difference between this and store is that-
store is for persistent data
 */

const sessionData = (function () {
  let revitModelName;
  let fileType;

  const getRevitModelName = function () {
    return revitModelName;
  };

  const setRevitModelName = function (name) {
    revitModelName = name;
  };

  const getFileType = function () {
    return fileType;
  };

  const setFileType = function (type) {
    fileType = type;
  };

  return {
    setRevitModelName,
    getRevitModelName,
    setFileType,
    getFileType,
  };
})();

module.exports = sessionData;
