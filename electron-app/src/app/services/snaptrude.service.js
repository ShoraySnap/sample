import axios from "axios";
import sessionData from "./sessionData";
import logger from "./logger";
import urls from "./urls";
import { PERSONAL_WORKSPACE_ID } from "../routes/constants";
import { keyBy } from "lodash";
import { CUSTOMER_LIFECYCLE, RequestType } from "./constants";

const snaptrudeService = (function () {
  const _callApi = async function (
    endPoint,
    requestType = RequestType.POST,
    data = {}
  ) {
    const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
    const formData = new FormData();
    for (let item in data) formData.append(item, data[item]);
    if (requestType === RequestType.POST) {
      return await _callByPostMethod(DJANGO_URL, endPoint, formData);
    } else if (requestType === RequestType.GET) {
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
          logger.log(res.data.message);
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
          logger.log(res.data.message);
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

  const checkModelUrl = async function (floorkey) {
    const data = {
      floorkey: floorkey,
    };
    const response = await _callApi(
      `/import/permission/?floorkey=${data.floorkey}`,
      RequestType.GET,
      data
    );
    return response?.status === 200;
  };

  const createProject = async function () {
    logger.log("Creating Snaptrude project");
    const endPoint = "/newBlankProject";
    const data = {
      project_name: sessionData.getUserData()["revitProjectName"],
    };

    const response = await _callApi(endPoint, RequestType.POST, data);
    if (response) {
      const floorkey = response.data.floorkey;
      logger.log("Created Snaptrude project", floorkey);

      return floorkey;
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

  const getFolders = async function (teamId, currentFolderId) {
    const fetchFromPersonalWorkspace = teamId === PERSONAL_WORKSPACE_ID;

    const endPoint = fetchFromPersonalWorkspace
      ? "/folderWithoutProject/"
      : `/team/${teamId}/folderWithoutProject/`;
    const data = {
      limit: 1000,
      offset: 0,
      folder: currentFolderId,
    };

    const response = await _callApi(endPoint, RequestType.POST, data);
    if (response) {
      return response.data.folders.map((folder) => {
        return {
          id: folder.id,
          name: folder.name,
        };
      });
    }
  };

  const checkRoleForPermissionToCreateProject = async (team) => {
    if (["viewer", "editor"].includes(team.role)) {
      return false;
    }
    if (!["admin", "creator"].includes(team.role)) {
      const endPoint = `/team/${team.id}/getrole/`;
      const response = await _callApi(endPoint, RequestType.POST, {});
      if (response.status === 200) {
        const permissionObject = response.data.team.permissions;
        const roleBasedPermissions = keyBy(permissionObject, (o) => o.name);
        if (!roleBasedPermissions[team.role].create_project) {
          return false;
        }
      }
    }
    return true;
  };

  const getValidTeams = async function (teams) {
    const validTeams = [];
    for (let i = 0; i < teams.length; ++i) {
      const team = teams[i];
      const isPermissionToCreateProject =
        await checkRoleForPermissionToCreateProject(team);
      if (!isPermissionToCreateProject) continue;
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

  const checkIfUserLoggedIn = async function () {
    const accessToken = sessionData.getUserData()["accessToken"];
    const refreshToken = sessionData.getUserData()["refreshToken"];

    const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
    const response = await axios.post(DJANGO_URL + "/refreshAccessToken/", {
      accessToken,
      refreshToken,
    });

    console.log("response is: ", response);

    if (response?.data?.accessToken) {
      sessionData.getUserData()["accessToken"] = response.data.accessToken;
      window.electronAPI.updateUserData(sessionData.getUserData());
      return true;
    }
    return false;
  };

  const checkPersonalWorkspaces = async function () {
    try {
      const isUserPro = await isPaidUserAccount();
      if (isUserPro) return true;

      const projectCount = await getProjectInPersonalWorkSpace();
      return projectCount < 5;
    } catch (error) {
      return false;
    }
  };

  const isPaidUserAccount = async function () {
    const endPoint = `/getuserprofile/`;
    try {
      const response = await _callApi(endPoint, RequestType.GET);
      if (!response) return false;

      const { isPro, customer_lifeCycle } = response.data;
      const isPaidUser = [
        CUSTOMER_LIFECYCLE.Paid_User,
        CUSTOMER_LIFECYCLE.Trial_Started,
      ].includes(customer_lifeCycle);
      return isPro || isPaidUser;
    } catch (error) {
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
    isPaidUserAccount,
    getFolders,
    checkIfUserLoggedIn,
    checkModelUrl,
  };
})();

export default snaptrudeService;
