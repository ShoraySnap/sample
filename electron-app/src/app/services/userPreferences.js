const userPreferences = (function () {
  let userPreferences = {};

  const get = function (key) {
    return userPreferences[key];
  };

  const set = function (key, data) {
    userPreferences[key] = data;
  };

  const setData = function (data) {
    userPreferences = data;
  };

  return {
    get,
    set,
    setData,
  };
})();

export default userPreferences;
