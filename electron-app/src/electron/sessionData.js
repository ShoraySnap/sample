/*
Difference between this and store is that-
store is for persistent data
 */

const sessionData = (function (){
  
  let revitModelName;
  
  const getRevitModelName = function (){
    return revitModelName;
  }
  
  const setRevitModelName = function (name){
    revitModelName = name;
  }
  
  return {
    setRevitModelName,
    getRevitModelName,
  }
  
})();

module.exports = sessionData;