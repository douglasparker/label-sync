const axios = require('axios');
const settings = require('./settings.json');

const forgejoApi = axios.create({
  baseURL: `${settings.url}/api/v1`,
  timeout: 3000,
  headers: {
    'Authorization': `token ${settings.apiKey}`,
    'Content-Type': 'application/json'
  }
});

module.exports = {
  forgejoApi,
  getRepositories: function () {
    return forgejoApi.get('/user/repos')
      .catch(function (error) {
        if(settings.logLevel === "error") console.error(error);
      });
  },
  createRepositoryLabel: function (repository, label) {
    return forgejoApi.post(`/repos/${repository.full_name}/labels`, {
      name: label.name,
      description: label.description,
      color: label.color,
      exclusive: label.exclusive,
      is_archived: label.archived
    }).catch(function (error) {
      if(settings.logLevel === "error") console.error(error);
    });
  },

  updateRepositoryLabel: function (repository, label, label_id) {
    return forgejoApi.patch(`/repos/${repository.full_name}/labels/${label_id}`, {
      name: label.name,
      description: label.description,
      color: label.color,
      exclusive: label.exclusive,
      is_archived: label.archived
    }).catch(function (error) {
      if(settings.logLevel === "error") console.error(error);
    });
  },

  deleteRespositoryLabel: function (repository, label) {
    return forgejoApi.delete(`/repos/${repository.full_name}/labels/${label.id}`)
      .catch(function (error) {
        if(settings.logLevel === "error") console.error(error);
      });
  },
  // :/repos/{owner}/{repo}/labels
  getRepositoryLabels: function (repository) {
    return forgejoApi.get(`/repos/${repository.full_name}/labels`, {
      limit: 10000//limit not working
    }).catch(function (error) {
      if(settings.logLevel === "error") console.error(error);
    });
  },

  purgeRepositoryLabels: function (repository) {
    return forgejoApi.get(`/repos/${repository.full_name}/labels`, {
      limit: 10000
    }).then(function (response) {
      response.data.forEach((label) => {
        module.exports.deleteRespositoryLabel(repository, label);
      });
    }).catch(function (error) {
      if(settings.logLevel === "error") console.error(error);
    });
  },
  purgeAllRepositoryLabels: async function () {
    let response = await module.exports.getRepositories();
    response.data.forEach(async (respository) => {
      await module.exports.purgeRepositoryLabels(respository);
    });
  }
}