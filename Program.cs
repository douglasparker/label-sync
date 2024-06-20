using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Models.Forgejo;
using Models.Database;
using Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http.Json;
using Enum;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LabelSync;

class Program
{
    private static Settings _settings;
    private static HttpClient _httpClient = new();
    private static List<Repository> _repositories;
    private static ApplicationContext _database = new ApplicationContext();
    static async Task Main(string[] args)
    {
        Console.WriteLine("LabelSync v0.0.0");

        Directory.CreateDirectory(AppContext.BaseDirectory + "data");

        if (!File.Exists(AppContext.BaseDirectory + "/data/settings.json"))
        {
            File.Copy("settings.template.json", AppContext.BaseDirectory + "/data/settings.json");
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(AppContext.BaseDirectory + "/data/settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        _settings = new Configuration.Settings();

        configuration.Bind("settings", _settings);

        if (!File.Exists(AppContext.BaseDirectory + "/data/labels.json"))
        {
            switch(_settings.Forge)
            {
                case (int)Enum.Forge.GitHub:
                    File.Copy("labels.github.template.json", AppContext.BaseDirectory + "/data/labels.json");
                    break;
                case (int)Enum.Forge.GitLab:
                    File.Copy("labels.gitlab.template.json", AppContext.BaseDirectory + "/data/labels.json");
                    break;
                case (int)Enum.Forge.Forgejo:
                    File.Copy("labels.forgejo.template.json", AppContext.BaseDirectory + "/data/labels.json");
                    break;
            }
        }

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        switch (_settings.Forge)
        {
            case (int)Forge.GitHub:
                Console.WriteLine("[INFO]: [FORGE]: GitHub");
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_settings.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "request");
                await _database.Database.OpenConnectionAsync();
                await _database.Database.MigrateAsync();
                List<Models.GitHub.Repository> repositories = await FilterGitHubRepositories();
                await CreateGitHubLabels(repositories);
                if(_settings.PurgeUndefinedLabels)
                {
                    await PurgeUndefinedGitHubLabels(repositories);
                }
                await _database.Database.CloseConnectionAsync();
                break;

            case (int)Forge.GitLab:
                Console.WriteLine("[INFO]: [FORGE]: GitLab");
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
                await _database.Database.OpenConnectionAsync();
                await _database.Database.MigrateAsync();
                List<Models.GitLab.Project> projects = await FilterGitLabRepositories();
                await CreateGitLabLabels(projects);
                if(_settings.PurgeUndefinedLabels)
                {
                    await PurgeUndefinedGitLabLabels(projects);
                }
                await _database.Database.CloseConnectionAsync();
                break;

            case (int)Forge.Bitbucket:
                Console.WriteLine("Bitbucket is not currently an implimented forge.");
                break;
                
            case (int)Forge.Forgejo:
                Console.WriteLine("[INFO]: [FORGE]: Forgejo");
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_settings.ApiKey}");
                await _database.Database.OpenConnectionAsync();
                await _database.Database.MigrateAsync();
                _repositories = await FilterRepositories();
                await CreateLabels();
                if(_settings.PurgeUndefinedLabels)
                {
                    await PurgeUndefinedForgejoLabels();
                }
                await _database.Database.CloseConnectionAsync();
                break;
        }
    }

    static async Task<List<Models.GitHub.Repository>> FilterGitHubRepositories()
    {
        if (_settings.Include != null && _settings.Include.Count > 0)
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: Include");
        }
        else if (_settings.Exclude != null && _settings.Exclude.Count > 0)
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: Exclude");
        }
        else
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: None");
        }

        List<Models.GitHub.Repository> repositories = await API.GitHub.GetRepositories(_httpClient, _settings);
        foreach (Models.GitHub.Repository repository in repositories.OrderBy(o => o.Full_Name))
        {
            // Include Filter:
            if (_settings.Include != null && _settings.Include.Count > 0)
            {
                if (!_settings.Include.Contains(repository.Full_Name))
                {
                    repositories.Remove(repository);
                }
                else
                {
                    Console.WriteLine($"[INFO]: [Include] {repository.Full_Name}");
                }
            }

            // Exclude Filter:
            else if (_settings.Exclude != null && _settings.Exclude.Count > 0)
            {
                if (_settings.Exclude.Contains(repository.Full_Name))
                {
                    repositories.Remove(repository);
                }
                else
                {
                    Console.WriteLine($"[INFO]: [Include] {repository.Full_Name}");
                }
            }

            // No Filter:
            else
            {
                Console.WriteLine($"[INFO]: [Include] {repository.Full_Name}");
            }
        }

        return repositories;
    }
    static async Task LinkGitHubLabels(List<Models.GitHub.Repository> repositories)
    {
        foreach (Models.GitHub.Repository repository in repositories.OrderBy(o => o.Full_Name))
        {
            foreach (Models.GitHub.Label label in await API.GitHub.GetRepositoryLabels(repository, _httpClient, _settings))
            {
                var labelSearch = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == repository.Id && o.LabelId == label.Id).FirstOrDefaultAsync<Models.Database.Label>();
                if (labelSearch == null)
                {
                    using FileStream openStream = File.OpenRead(AppContext.BaseDirectory + "/data/labels.json");
                    // Do NOT index <List<Configuration.Label>>. The index of each label is crucial to linking repository labels to changes.
                    List<Configuration.Label> customLabels = await JsonSerializer.DeserializeAsync<List<Configuration.Label>>(openStream);

                    foreach (Configuration.Label customLabel in customLabels)
                    {
                        if (label.Name == customLabel.Name)
                        {
                            await _database.AddAsync<Models.Database.Label>(new Models.Database.Label
                            {
                                IndexId = customLabels.IndexOf(customLabel),
                                LabelId = label.Id,
                                LabelName = label.Name,
                                RepositoryId = repository.Id
                            });
                            Console.WriteLine($"[INFO]: [LINK] {repository.Full_Name} (ID: {label.Id})");
                        }
                    }
                }
            }
        }
        await _database.SaveChangesAsync();
    }
    static async Task CreateGitHubLabels(List<Models.GitHub.Repository> repositories)
    {
        await LinkGitHubLabels(repositories);
        foreach (Models.GitHub.Repository repository in repositories.OrderBy(o => o.Full_Name))
        {
            using FileStream openStream = File.OpenRead(AppContext.BaseDirectory + "/data/labels.json");
            // Do NOT index <List<Configuration.Label>>. The index of each label is crucial to linking repository labels to changes.
            List<Configuration.GitHub.Label> customLabels = await JsonSerializer.DeserializeAsync<List<Configuration.GitHub.Label>>(openStream);

            foreach (Configuration.GitHub.Label customLabel in customLabels)
            {
                var labelSearch = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == repository.Id && o.IndexId == customLabels.IndexOf(customLabel)).FirstOrDefaultAsync<Models.Database.Label>();
                if (labelSearch == null)
                {
                    Models.GitHub.Label label = new Models.GitHub.Label();
                    label.Name = customLabel.Name;
                    label.Description = customLabel.Description;
                    label.Color = customLabel.Color;
                    HttpResponseMessage response = await API.GitHub.CreateRepositoryLabel(repository, label, _httpClient, _settings);
                    if (response.IsSuccessStatusCode)
                    {
                        Models.GitHub.Label newLabel = await response.Content.ReadFromJsonAsync<Models.GitHub.Label>();
                        Console.WriteLine($"[INFO]: [CREATE] {repository.Full_Name} (ID: {newLabel.Id})");
                    }
                    else
                    {
                        Console.WriteLine($"[{response.StatusCode}] " + response.Content.ReadAsStringAsync().Result);
                    }


                }
                else
                {
                    Models.GitHub.Label label = new Models.GitHub.Label();
                    label.Name = customLabel.Name;
                    label.New_Name = label.Name;
                    label.Description = customLabel.Description;
                    label.Color = customLabel.Color;
                    HttpResponseMessage response = await API.GitHub.UpdateRepositoryLabel(repository, label, labelSearch.LabelName, _httpClient, _settings);
                    Console.WriteLine($"[INFO]: [UPDATE] {repository.Full_Name} (ID: {labelSearch.LabelId})");
                }
            }
        }
        await LinkGitHubLabels(repositories);
    }
    static async Task PurgeUndefinedGitHubLabels(List<Models.GitHub.Repository> repositories)
    {
        foreach (Models.GitHub.Repository repository in repositories.OrderBy(o => o.Full_Name))
        {
            foreach (Models.GitHub.Label label in await API.GitHub.GetRepositoryLabels(repository, _httpClient, _settings))
            {
                var isDefinedLabel = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == repository.Id && o.LabelId == label.Id).FirstOrDefaultAsync<Models.Database.Label>();
                if (isDefinedLabel == null)
                {
                    await API.GitHub.DeleteRepositoryLabel(repository, label.Name, _httpClient, _settings);
                    Console.WriteLine($"[INFO]: [DELETE] {repository.Full_Name} (ID: {label.Id})");
                }
            }
        }
    }
    static async Task<List<Models.GitLab.Project>> FilterGitLabRepositories()
    {
        if (_settings.Include != null && _settings.Include.Count > 0)
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: Include");
        }
        else if (_settings.Exclude != null && _settings.Exclude.Count > 0)
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: Exclude");
        }
        else
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: None");
        }

        List<Models.GitLab.Project> projects = await API.GitLab.GetProjects(_httpClient, _settings);
        foreach (Models.GitLab.Project project in projects.OrderBy(o => o.Path_With_Namespace))
        {
            // Include Filter:
            if (_settings.Include != null && _settings.Include.Count > 0)
            {
                if (!_settings.Include.Contains(project.Path_With_Namespace))
                {
                    projects.Remove(project);
                }
                else
                {
                    Console.WriteLine($"[INFO]: [Include] {project.Path_With_Namespace}");
                }
            }

            // Exclude Filter:
            else if (_settings.Exclude != null && _settings.Exclude.Count > 0)
            {
                if (_settings.Exclude.Contains(project.Path_With_Namespace))
                {
                    projects.Remove(project);
                }
                else
                {
                    Console.WriteLine($"[INFO]: [Include] {project.Path_With_Namespace}");
                }
            }

            // No Filter:
            else
            {
                Console.WriteLine($"[INFO]: [Include] {project.Path_With_Namespace}");
            }
        }

        return projects;
    }

    static async Task LinkGitLabLabels(List<Models.GitLab.Project> projects)
    {
        foreach (Models.GitLab.Project project in projects.OrderBy(o => o.Path_With_Namespace))
        {
            foreach (Models.GitLab.Label label in await API.GitLab.GetProjectLabels(project, _httpClient, _settings))
            {
                var labelSearch = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == project.Id && o.LabelId == label.Id).FirstOrDefaultAsync<Models.Database.Label>();
                if (labelSearch == null)
                {
                    using FileStream openStream = File.OpenRead(AppContext.BaseDirectory + "/data/labels.json");
                    // Do NOT index <List<Configuration.Label>>. The index of each label is crucial to linking repository labels to changes.
                    List<Configuration.Label> customLabels = await JsonSerializer.DeserializeAsync<List<Configuration.Label>>(openStream);

                    foreach (Configuration.Label customLabel in customLabels)
                    {
                        if (label.Name == customLabel.Name)
                        {
                            await _database.AddAsync<Models.Database.Label>(new Models.Database.Label
                            {
                                IndexId = customLabels.IndexOf(customLabel),
                                LabelId = label.Id,
                                RepositoryId = project.Id
                            });
                            Console.WriteLine($"[INFO]: [LINK] {project.Path_With_Namespace} (ID: {label.Id})");
                        }
                    }
                }
            }
        }
        await _database.SaveChangesAsync();
    }
    
    static async Task CreateGitLabLabels(List<Models.GitLab.Project> projects)
    {
        await LinkGitLabLabels(projects);
        foreach (Models.GitLab.Project project in projects.OrderBy(o => o.Path_With_Namespace))
        {
            using FileStream openStream = File.OpenRead(AppContext.BaseDirectory + "/data/labels.json");
            // Do NOT index <List<Configuration.Label>>. The index of each label is crucial to linking repository labels to changes.
            List<Configuration.GitLab.Label> customLabels = await JsonSerializer.DeserializeAsync<List<Configuration.GitLab.Label>>(openStream);

            foreach (Configuration.GitLab.Label customLabel in customLabels)
            {
                var labelSearch = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == project.Id && o.IndexId == customLabels.IndexOf(customLabel)).FirstOrDefaultAsync<Models.Database.Label>();
                if (labelSearch == null)
                {
                    Models.GitLab.Label label = new Models.GitLab.Label();
                    label.Name = customLabel.Name;
                    label.Description = customLabel.Description;
                    label.Color = customLabel.Color;
                    label.Priority = customLabel.Priority;
                    HttpResponseMessage response = await API.GitLab.CreateProjectLabel(project, label, _httpClient, _settings);
                    if (response.IsSuccessStatusCode)
                    {
                        Models.GitLab.Label newLabel = await response.Content.ReadFromJsonAsync<Models.GitLab.Label>();
                        Console.WriteLine($"[INFO]: [CREATE] {project.Path_With_Namespace} (ID: {newLabel.Id})");
                    }
                    else
                    {
                        Console.WriteLine($"[{response.StatusCode}] " + response.Content.ReadAsStringAsync().Result);
                    }


                }
                else
                {
                    Models.GitLab.Label label = new Models.GitLab.Label();
                    label.Name = customLabel.Name;
                    label.New_Name = label.Name;
                    label.Description = customLabel.Description;
                    label.Color = customLabel.Color;
                    label.Priority = customLabel.Priority;
                    HttpResponseMessage response = await API.GitLab.UpdateProjectLabel(project, label, labelSearch.LabelId, _httpClient, _settings);
                    Console.WriteLine($"[INFO]: [UPDATE] {project.Path_With_Namespace} (ID: {labelSearch.LabelId})");
                }
            }
        }
        await LinkGitLabLabels(projects);
    }

    static async Task PurgeUndefinedGitLabLabels(List<Models.GitLab.Project> projects)
    {
        foreach (Models.GitLab.Project project in projects.OrderBy(o => o.Path_With_Namespace))
        {
            foreach (Models.GitLab.Label label in await API.GitLab.GetProjectLabels(project, _httpClient, _settings))
            {
                var isDefinedLabel = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == project.Id && o.LabelId == label.Id).FirstOrDefaultAsync<Models.Database.Label>();
                if (isDefinedLabel == null)
                {
                    await API.GitLab.DeleteProjectLabel(project, label.Id, _httpClient, _settings);
                    Console.WriteLine($"[INFO]: [DELETE] {project.Path_With_Namespace} (ID: {label.Id})");
                }
            }
        }
    }

    static async Task<List<Repository>> FilterRepositories()
    {
        if (_settings.Include != null && _settings.Include.Count > 0)
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: Include");
        }
        else if (_settings.Exclude != null && _settings.Exclude.Count > 0)
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: Exclude");
        }
        else
        {
            Console.WriteLine("[INFO]: [FILTER TYPE]: None");
        }

        List<Repository> repositories = await API.Forgejo.GetRepositories(_httpClient, _settings);
        foreach (var repo in repositories.OrderBy(o => o.Full_Name))
        {
            // Include Filter:
            if (_settings.Include != null && _settings.Include.Count > 0)
            {
                if (!_settings.Include.Contains(repo.Full_Name))
                {
                    repositories.Remove(repo);
                }
                else
                {
                    Console.WriteLine($"[INFO]: [Include] {repo.Full_Name}");
                }
            }

            // Exclude Filter:
            else if (_settings.Exclude != null && _settings.Exclude.Count > 0)
            {
                if (_settings.Exclude.Contains(repo.Full_Name))
                {
                    repositories.Remove(repo);
                }
                else
                {
                    Console.WriteLine($"[INFO]: [Include] {repo.Full_Name}");
                }
            }

            // No Filter:
            else
            {
                Console.WriteLine($"[INFO]: [Include] {repo.Full_Name}");
            }
        }

        return repositories;
    }

    /// <summary>
    /// Link and add previously created repository labels in a Forgejo instance to the Label Sync database.
    /// </summary>
    /// <returns></returns>
    static async Task LinkLabels()
    {
        foreach (var repo in _repositories.OrderBy(o => o.Full_Name))
        {
            foreach (var label in await API.Forgejo.GetRepositoryLabels(repo, _httpClient, _settings))
            {
                var labelSearch = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == repo.Id && o.LabelId == label.Id).FirstOrDefaultAsync<Models.Database.Label>();
                if (labelSearch == null)
                {
                    using FileStream openStream = File.OpenRead(AppContext.BaseDirectory + "/data/labels.json");
                    // Do NOT index <List<Configuration.Label>>. The index of each label is crucial to linking repository labels to changes.
                    List<Configuration.Label> customLabels = await JsonSerializer.DeserializeAsync<List<Configuration.Label>>(openStream);

                    foreach (Configuration.Label customLabel in customLabels)
                    {
                        if (label.Name == customLabel.Name)
                        {
                            await _database.AddAsync<Models.Database.Label>(new Models.Database.Label
                            {
                                IndexId = customLabels.IndexOf(customLabel),
                                LabelId = label.Id,
                                RepositoryId = repo.Id
                            });
                            Console.WriteLine($"[INFO]: [LINK] {repo.Full_Name} (ID: {label.Id})");
                        }
                    }
                }
            }
        }
        await _database.SaveChangesAsync();
    }
    static async Task CreateLabels()
    {
        await LinkLabels();
        foreach (var repo in _repositories.OrderBy(o => o.Full_Name))
        {
            using FileStream openStream = File.OpenRead(AppContext.BaseDirectory + "/data/labels.json");
            // Do NOT index <List<Configuration.Label>>. The index of each label is crucial to linking repository labels to changes.
            List<Configuration.Label> customLabels = await JsonSerializer.DeserializeAsync<List<Configuration.Label>>(openStream);

            foreach (Configuration.Label customLabel in customLabels)
            {
                var labelSearch = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == repo.Id && o.IndexId == customLabels.IndexOf(customLabel)).FirstOrDefaultAsync<Models.Database.Label>();
                if (labelSearch == null)
                {
                    Models.Forgejo.Label label = new Models.Forgejo.Label();
                    label.Name = customLabel.Name;
                    label.Description = customLabel.Description;
                    label.Color = customLabel.Color;
                    label.Exclusive = customLabel.Exclusive;
                    label.Is_Archived = customLabel.Archived;
                    HttpResponseMessage response = await API.Forgejo.CreateRepositoryLabel(repo, label, _httpClient, _settings);
                    Models.Forgejo.Label newLabel = await response.Content.ReadFromJsonAsync<Models.Forgejo.Label>();
                    Console.WriteLine($"[INFO]: [CREATE] {repo.Full_Name} (ID: {newLabel.Id})");
                }
                else
                {
                    Models.Forgejo.Label label = new Models.Forgejo.Label();
                    label.Name = customLabel.Name;
                    label.Description = customLabel.Description;
                    label.Color = customLabel.Color;
                    label.Exclusive = customLabel.Exclusive;
                    label.Is_Archived = customLabel.Archived;
                    HttpResponseMessage response = await API.Forgejo.UpdateRepositoryLabel(repo, label, labelSearch.LabelId, _httpClient, _settings);
                    Console.WriteLine($"[INFO]: [UPDATE] {repo.Full_Name} (ID: {labelSearch.LabelId})");
                }
            }
        }
        await LinkLabels();
    }

    static async Task PurgeUndefinedForgejoLabels()
    {
        foreach (var repo in _repositories.OrderBy(o => o.Full_Name))
        {
            foreach (var label in await API.Forgejo.GetRepositoryLabels(repo, _httpClient, _settings))
            {
                var isDefinedLabel = await _database.Labels.Where<Models.Database.Label>(o => o.RepositoryId == repo.Id && o.LabelId == label.Id).FirstOrDefaultAsync<Models.Database.Label>();
                if (isDefinedLabel == null)
                {
                    await API.Forgejo.DeleteRepositoryLabel(repo, label.Id, _httpClient, _settings);
                    Console.WriteLine($"[INFO]: [DELETE] {repo.Full_Name} (ID: {label.Id})");
                }
            }
        }
    }
}
