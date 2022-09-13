const axios = require("axios");
const sessionData = require("../sessionData");
const store = require("../store");
const logger = require("../services/logger");
const urls = require("./urls");

const speckleService = (function (){
  
  const _callApi = async function (query, variables){
    const API_URL = urls.get("speckleUrl") + "/graphql";
    const apiToken = store.get('speckleApiToken');

    return await axios.post(
      API_URL,
      {
        query: query,
        variables: variables,
      },
      {
        headers: {
          Authorization: "Bearer " + apiToken,
        },
      }
    ).then(res => {
      if (res.status !== 200) {
        logger.log("Server error");
        logger.log(res.status);
        logger.log(res.statusText);
        return null;
      }
      else {
        return res;
      }
    }).catch(e => {
      logger.log("Network error");
      logger.log(e);
    });
    
  }
  
  const generateStreamId = async () => {
    const query = `mutation CreateStream($stream: StreamCreateInput!){
      streamCreate (stream : $stream)
    }`;
    
    const revitModelName = store.get('revitProjectName');
  
    const variables = {
      stream : {
        name: revitModelName,
        description: `Exporting ${revitModelName} to Snaptrude`,
        isPublic: false,
      }
    };
    
    const response = await _callApi(query, variables);
  
    if (response) return response.data.data.streamCreate;
  };
  
  const _fetchLatestCommitId = async (streamId) => {
    const query = `query GetLatestCommit($streamId: String!){
      stream(id : $streamId){
        branch(name : "main"){
          commits(limit: 1){
            items{
              id
            }
          }
        }
      }
    }`;
  
    const variables = {
      streamId
    };
    
    const response = await _callApi(query, variables);
  
    if (response) return response.data.data.stream.branch.commits.items[0]?.id;
  };
  
  const startPolling = async function (){
    
    logger.log("Polling for speckle upload completion");
    const streamId = store.get('streamId');
    
    const timeout = 15 * 60 * 1e3;
    const pollInterval = 10 * 1e3;
    
    return new Promise((resolve, reject) => {
      
      let timeoutId, pollerId;
      let updateDone = false;
      
      pollerId = setInterval(async () => {
      
        const commitId = await _fetchLatestCommitId(streamId);
        
        if (commitId) {
          logger.log("Speckle upload completed");
          updateDone = true;
          clearTimeout(timeoutId);
          clearInterval(pollerId);
          resolve(true);
        }
      }, pollInterval);
      
      timeoutId = setTimeout(() => {
        if (!updateDone){
          logger.log("Stopping speckle polling");
          clearInterval(pollerId);
          resolve(false);
        }
      }, timeout)
      
    });
    
  };
  
  return {
    generateStreamId,
    startPolling,
  }
  
})();

module.exports = speckleService;