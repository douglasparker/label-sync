using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Models.Forgejo;
using Models.Database;
using Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http.Json;

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

        if (!File.Exists(AppContext.BaseDirectory + "\\settings.json"))
        {
            File.Copy("settings.template.json", AppContext.BaseDirectory + "settings.json");
        }

        Directory.CreateDirectory(AppContext.BaseDirectory + "database");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        _settings = new Configuration.Settings();

        configuration.Bind("settings", _settings);

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("Authorization", "token 07ca309c661275b91ee989c8c6ff133fba838dbf");

        await _database.Database.OpenConnectionAsync();
        await _database.Database.MigrateAsync();
        _repositories = await FilterRepositories();
        await CreateLabels();
        await _database.Database.CloseConnectionAsync();
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
                    using FileStream openStream = File.OpenRead("labels.json");
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
            using FileStream openStream = File.OpenRead("labels.json");
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
                    Console.WriteLine($"[INFO]: [CREATE] ${repo.Full_Name} (ID: ${newLabel.Id})");
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
}
