const { PrismaClient } = require('@prisma/client');

const settings = require('./settings.json');

const labels = require('./labels.json');

const api = require('./api.js');

const prisma = new PrismaClient()

let repositories = [];

// TODO: Convert to .NET Core
// Bug: Labels that already exist are created again (might have to do with too many clients error or asynchronous calls, idk. Might fix itself when rewritten in .NET)
// save this code as nodejs/prototype branch

async function main() {
  if (settings.username == "" && settings.organizations.length == 0) {
    console.log('You must set at least a username or array of organizations that you want to sync labels to.');
  }
  else {
    await api.purgeAllRepositoryLabels();
    //await prepareRepositories();
    //await linkLabels();
    //await createLabels();
  }
}

async function prepareRepositories() {
  // Generate repositories list based on include / exclude rules.
  console.log('[INFO]: Generating repository list...');
  let response = await api.getRepositories();
  response.data.forEach((respository) => {
    if (settings.include.length > 0) {
      if (settings.include.includes(respository.full_name)) {
        console.log('[Include]: ' + respository.full_name)
        repositories.push(respository);
      }
    }
    else if (settings.exclude.length > 0) {
      if (settings.exclude.includes(respository.full_name)) {
        console.log('[Exclude]: ' + respository.full_name)
      }
      else {
        console.log('[Include]: ' + respository.full_name)
        repositories.push(respository);
      }
    }
    else {
      console.log('[Include]: ' + respository.full_name)
      repositories.push(respository);
    }
  })
  // if debug: console.log(repositories);
  console.log('[INFO]: Repository list generation complete.');
}

async function linkLabels() {
  repositories.forEach(async (repository) => {
    var repositoryLabels = await api.getRepositoryLabels(repository);
    repositoryLabels.data.forEach(async (repositoryLabel) => {
      const query = await prisma.label.findFirst({
        where: {
          repository: parseInt(repository.id),
          AND: [
            { label: repositoryLabel.id }
          ]
        }
      });
      if (!query) {
        labels.forEach(async (label, index) => {
          if (repositoryLabel.name === label.name) {
            const query = await prisma.label.create({
              data: {
                index: index,
                label: repositoryLabel.id,
                repository: parseInt(repository.id),
              }
            });
            console.log(`[Link]: Repository: ${parseInt(repository.id)} Label ID: ${repositoryLabel.id}`);
          }
        });
      }
    })
  });
}

async function createLabels() {
  repositories.forEach(async (repository) => {
    labels.forEach(async (label, index) => {
      // Check if linked index exists
      const query = await prisma.label.findFirst({
        where: {
          repository: parseInt(repository.id),
          AND: [
            { index: index }
          ]
        }
      });
      // If it exists, update
      if (query != null) {
        const response = await api.updateRepositoryLabel(repository, label, query.label);
        console.log(`[Update]: Repository: ${query.repository} Label ID: ${query.label}`);
      }
      // otherwise make new
      else {
        const response = await api.createRepositoryLabel(repository, label);
        const query = await prisma.label.create({
          data: {
            index: index,
            label: response.data.id,
            repository: parseInt(repository.id),
          }
        });
        console.log(`[Create]: Label Index: ${index} Repository: ${response.data.repository} Label ID: ${response.data.label_id}`);
      }
    })
  })
}

main()
  .then(async () => {
    await prisma.$disconnect()
  })
  .catch(async (e) => {
    console.error(e)
    await prisma.$disconnect()
    process.exit(1)
  })