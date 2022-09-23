import axios from "axios";
import sessionData from "./sessionData";
import logger from "./logger";
import urls from "./urls";

const snaptrudeService = (function () {
  const RequestType = {
    GET: "GET",
    POST: "POST",
  };

  const _callApi = async function (
    endPoint,
    requestType = RequestType.POST,
    data = {}
  ) {
    const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
    const formData = new FormData();
    for (let item in data) formData.append(item, data[item]);
    if (requestType == RequestType.POST) {
      return await _callByPostMethod(DJANGO_URL, endPoint, formData);
    } else if (requestType == RequestType.GET) {
      return await _callByGetMethod(DJANGO_URL, endPoint);
    }
  };

  const _callByPostMethod = async (DJANGO_URL, endPoint, formData) => {
    return axios
      .post(DJANGO_URL + endPoint, formData)
      .then((res) => {
        if (res.status !== 200 && res.status !== 202) {
          logger.log("Server error");
          logger.log(res.status);
          logger.log(res.statusText);
          return null;
        } else if (res.data.error) {
          logger.log("Server error");
          logger.log(res.data.error);
          return null;
        } else {
          return res;
        }
      })
      .catch((e) => {
        logger.log("Network error");
        const res = e?.response;
        if (res)
          logger.log(
            res.status,
            res.statusText,
            res.data.message,
            res.data.error
          );
        else logger.log("Empty error msg", endPoint);
      });
  };

  const _callByGetMethod = async (DJANGO_URL, endPoint) => {
    return await axios
      .get(DJANGO_URL + endPoint)
      .then((res) => {
        if (res.status !== 200 && res.status !== 202) {
          logger.log("Server error");
          logger.log(res.status);
          logger.log(res.statusText);
          return null;
        } else if (res.data.error) {
          logger.log("Server error");
          logger.log(res.data.error);
          return null;
        } else {
          return res;
        }
      })
      .catch((e) => {
        logger.log("Network error");
        const res = e?.response;
        if (res)
          logger.log(
            res.status,
            res.statusText,
            res.data.message,
            res.data.error
          );
        else logger.log("Empty error msg", endPoint);
      });
  };

  const createProject = async function (streamId, teamId) {
    logger.log("Creating Snaptrude project for", streamId, teamId);
    const REACT_URL = urls.get("snaptrudeReactUrl");

    const endPoint = "/newSpeckleLinkedBlankProject";
    const data = {
      stream_id: streamId,
      team_id: teamId,
      project_name: sessionData.getUserData()["revitProjectName"],
    };

    const response = await _callApi(endPoint, RequestType.POST, data);
    if (response) {
      const floorkey = response.data.floorkey;
      logger.log("Created Snaptrude project", floorkey);

      return REACT_URL + "/model/" + floorkey;
    }
  };

  const getUserWorkspaces = async function () {
    const endPoint = "/user/teams/active";
    const response = await _callApi(endPoint);
    if (response) {
      const validTeams = await getValidTeams(response.data.teams);
      return validTeams.map((team) => {
        return {
          id: team.id,
          name: team.name,
        };
      });
    }
  };

  const getValidTeams = async function (teams) {
    const validTeams = [];
    for (let i = 0; i < teams.length; ++i) {
      const team = teams[i];
      if (team.isManuallyPaid) {
        validTeams.push(team);
        continue;
      }
      const endPoint = `/team/${team.id}/project?`;
      const data = {
        limit: 10,
        offset: 0,
      };
      let queryParams = new URLSearchParams();
      for (let item in data) {
        queryParams.append(item, data[item]);
      }
      queryParams = queryParams.toString();
      const response = await _callApi(
        endPoint + queryParams,
        RequestType.GET,
        data
      );
      if (response) {
        const projectsCount = response.data.projects.length;
        if (projectsCount < 1) {
          validTeams.push(team);
        }
      }
    }
    return validTeams;
  };

  const checkPersonalWorkspaces = async function () {
    const endPoint = "/payments/ispro";
    const response = await _callApi(endPoint, RequestType.GET);
    // console.log(response);
    if (response) {
      const isPro = response.data.isPro;
      if (isPro) return true;
      const projectCount = await getProjectInPersonalWorkSpace();
      if (projectCount < 3) {
        return true;
      }
      return false;
    }
  };

  const getProjectInPersonalWorkSpace = async () => {
    const endPoint = "/getprojects/";
    const data = {
      limit: 10,
      offset: 0,
    };
    const response = await _callApi(endPoint, RequestType.POST, data);
    if (response) {
      const projectCount = response.data.length;
      return projectCount;
    }
  };

  axios.interceptors.request.use(
    (request) => {
      const ignorePaths = [
        "/register/",
        "/snaplogin/",
        "/refreshAccessToken/",
        "/sendResetPasswordMail/",
        "/resetPassword/",
      ];
      const path = new URL(request.url).pathname;
      if (ignorePaths.includes(path)) return request;
      const accessToken = sessionData.getUserData()["accessToken"];
      request.headers.Auth = "Bearer " + accessToken;
      return request;
    },
    (err) => {
      console.log(err);
      return Promise.reject();
    }
  );

  axios.interceptors.response.use(
    async (response) => {
      if (response.data.error && response.data.isTokenExpired) {
        // HANDLE TOKEN EXPIRED
        const accessToken = sessionData.getUserData()["accessToken"];
        const refreshToken = sessionData.getUserData()["refreshToken"];

        const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
        // GET NEW ACCESS TOKEN.
        const resp = await axios.post(DJANGO_URL + "/refreshAccessToken/", {
          accessToken,
          refreshToken,
        });

        if (resp.data.accessToken) {
          // UPDATE ACCESS TOKEN AND RETRY THE ORIGINAL REQUEST.

          sessionData.getUserData()["accessToken"] = resp.data.accessToken;
          window.electronAPI.updateUserData(sessionData.getUserData());

          const originalResponse = await axios(response.config);

          if (!originalResponse.data.error) return originalResponse;
        }
        return resp;
      } else if (response.data.error === 2) {
        localStorage.removeItem("user");
        localStorage.removeItem("refreshToken");
      }
      return response;
    },
    (err) => {
      console.log(err);
      return Promise.reject();
    }
  );

  return {
    createProject,
    getUserWorkspaces,
    checkPersonalWorkspaces,
  };
})();

export default snaptrudeService;
