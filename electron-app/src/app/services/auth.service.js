const authService = (function () {
  let userData = {};

  const init = function (data) {
    userData = data;
  };

  const flush = function () {
    userData = {};
  };

  return {
    init,
    flush,
  };
})();

export default authService;
