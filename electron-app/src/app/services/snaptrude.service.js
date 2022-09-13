import axios from "axios";
import sessionData from "./sessionData";
import logger from "./logger";
import urls from "./urls";

const snaptrudeService = (function (){
  
  const _callApi = async function (endPoint, data = {}){
    const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
    const apiToken = sessionData.getUserData()['accessToken'];
    
    const formData = new FormData();
    for(let item in data) formData.append(item, data[item]);

    return await axios.post(
      DJANGO_URL + endPoint,
      formData,
      {
        headers: {
          Auth: "Bearer " + apiToken,
        },
      }
    ).then(res => {
      if (res.status !== 200 && res.status !== 202) {
        logger.log("Server error");
        logger.log(res.status);
        logger.log(res.statusText);
        return null;
      }
      else if (res.data.error){
        logger.log("Server error");
        logger.log(res.data.error);
        return null;
      }
      else {
        return res;
      }
    }).catch(e => {
      logger.log("Network error");
      const res = e?.response;
      if (res) logger.log(res.status, res.statusText, res.data.message, res.data.error);
      else logger.log("Empty error msg", endPoint);
    });
  }
  
  const createProject = async function (streamId, teamId){
    logger.log("Creating Snaptrude project for", streamId, teamId);
    const REACT_URL = urls.get("snaptrudeReactUrl");
    
    const endPoint = "/newSpeckleLinkedBlankProject";
    const data = {
      stream_id: streamId,
      team_id: teamId,
      project_name: sessionData.getUserData()["revitProjectName"],
    }
    
    const response = await _callApi(endPoint, data);
    if (response) {
      const floorkey = response.data.floorkey;
      logger.log("Created Snaptrude project", floorkey);
      
      return REACT_URL + "/model/" + floorkey
    }
  };
  
  
  const getUserWorkspaces = async function (){
    const endPoint = "/user/teams/active";
    const response = await _callApi(endPoint);
    if (response) {
      return response.data.teams.map(team => {
        return {
          id: team.id,
          name: team.name,
        }
      });
    }
  }
  
  axios.interceptors.response.use(
  async (response) => {
    if (response.data.error && response.data.isTokenExpired) {
      // HANDLE TOKEN EXPIRED
      const accessToken = sessionData.getUserData()['accessToken'];
      const refreshToken = sessionData.getUserData()['refreshToken'];

      const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
      // GET NEW ACCESS TOKEN.
      const resp = await axios.post(
        DJANGO_URL + "/refreshAccessToken/",
        {
          accessToken,
          refreshToken,
        }
      );

      if (resp.data.accessToken) {
        // UPDATE ACCESS TOKEN AND RETRY THE ORIGINAL REQUEST.

        sessionData.getUserData()['accessToken'] = resp.data.accessToken;
        window.electronAPI.updateUserData(sessionData.getUserData());

        const originalResponse = await axios(response.config);

        if (!originalResponse.data.error) return originalResponse;
      }
      return resp;
    }else if(response.data.error === 2) {
      localStorage.removeItem("user");
      localStorage.removeItem("refreshToken");
    }
    return response;
  },
  (err) => {
    console.log(err)
    return Promise.reject();
  }
);
  
  return {
    createProject,
    getUserWorkspaces,
  };
  
})();

export default snaptrudeService;