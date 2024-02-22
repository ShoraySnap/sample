export const RouteStore = (function (){
  let data = {};
  
  const get = function (key) {
    return data[key];
  }
  
  const set = function (key, val) {
    data[key] = val;
  }
  
  return {
    get,
    set
  }
})();