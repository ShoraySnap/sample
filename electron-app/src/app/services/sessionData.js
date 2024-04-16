const sessionData = (function () {
  let userData = {};

  const getUserData = function () {
    return userData;
  };

  const setUserData = function (data) {
    userData = data;
  };

  const flush = function () {
    userData = {};
  };

  return {
    setUserData,
    getUserData,
    flush,
  };
})();

export default sessionData;
