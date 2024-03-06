const axios = require("axios");
const urls = require("./urls");
const logger = require("./logger");
const store = require("../store");
const FormData = require("form-data");
const sessionData = require("../sessionData");


const snaptrudeService = function () {
  const RequestType = {
    GET: "GET",
    POST: "POST",
  };
  const _callApi = async function (endPoint, requestType = RequestType.POST, data = {}) {
    const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
    const formData = new URLSearchParams();
    for (const item in data) {
      if (Object.hasOwn(data, item)) {
        formData.append(item, data[item]);
      }
    }
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

  const createProject = async function (teamId, folderId) {
    logger.log("Creating Snaptrude project");
    const REACT_URL = urls.get("snaptrudeReactUrl");

    const endPoint = "/newSpeckleLinkedBlankProject";
    // const data = {
    //   project_name: store.get('revitProjectName'),
    // };

    const data = {
      project_name: sessionData.getRevitModelName(),
      stream_id: "test",
      team_id: teamId,
      folder_id: folderId,
    }

    const response = await _callApi(endPoint, RequestType.POST, data);
    if (response) {
      const floorkey = response.data.floorkey;
      logger.log("Created Snaptrude project", floorkey);

      return floorkey;
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
      const accessToken = store.get("accessToken");
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
        const accessToken = store.get("accessToken");
        const refreshToken = store.get("refreshToken");

        const DJANGO_URL = urls.get("snaptrudeDjangoUrl");
        // GET NEW ACCESS TOKEN.
        const resp = await axios.post(DJANGO_URL + "/refreshAccessToken/", {
          accessToken,
          refreshToken,
        });

        if (resp.data.accessToken) {
          // UPDATE ACCESS TOKEN AND RETRY THE ORIGINAL REQUEST.

          store.set("accessToken", resp.data.accessToken);

          const originalResponse = await axios(response.config);

          if (!originalResponse.data.error) return originalResponse;
        }
        return resp;
      } else if (response.data.error === 2) {
        // localStorage.removeItem("user");
        // localStorage.removeItem("refreshToken");
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
  };
}();

module.exports = snaptrudeService;
