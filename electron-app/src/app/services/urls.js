const urls = (function () {
  let _urls;

  const init = function (data) {
    _urls = data;
  };

  const get = function (key) {
    return _urls[key];
  };

  return {
    init,
    get,
  };
})();

export default urls;
