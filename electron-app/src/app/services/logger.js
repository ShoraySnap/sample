const logger = (function () {
  const log = function (...messages) {
    window.electronAPI.log(messages);
  };

  return {
    log,
  };
})();

export default logger;
