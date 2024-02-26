using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Models.ForgejoAPI;
using Configuration;

namespace LabelSync;

class Program
{
    private static Settings _settings;
    private static HttpClient _httpClient = new();
    private static List<Repository> _repositories;
    static async Task Main(string[] args)
    {
        Console.WriteLine("LabelSync v0.0.0");

        if(!File.Exists(AppContext.BaseDirectory + "\\settings.json")) {
            File.Copy("settings.template.json", AppContext.BaseDirectory + "settings.json");
        }

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

        await FilterRepositories();

        // Single
        /*
        Repository r = await API.Forgejo.GetRepository("douglasparker/.profile", _httpClient, _settings);
        Console.WriteLine(r.Full_Name);
        */
        
        // List
        /*
        List<Repository> repositories = await API.Forgejo.GetRepositories(_httpClient, _settings);
        foreach(var item in repositories.OrderBy(o => o.Id)) {
            Console.WriteLine(item.Id);
        }
        */
        
    }

    static async Task<List<Repository>> FilterRepositories()
    {
        Console.WriteLine("[INFO]: Filtering Repositories...");

        if(_settings.Include != null && _settings.Include.Count > 0) {
            Console.WriteLine("[INFO]: Filter Type: Include");
        }
        else if(_settings.Exclude != null && _settings.Exclude.Count > 0) {
            Console.WriteLine("[INFO]: Filter Type: Exclude");
        }
        else {
            Console.WriteLine("[INFO]: Filter Type: None");
        }

        List<Repository> repositories = await API.Forgejo.GetRepositories(_httpClient, _settings);
        foreach(var repo in repositories.OrderBy(o => o.Full_Name)) {

            // Include Filter:
            if(_settings.Include != null && _settings.Include.Count > 0) {
                if(!_settings.Include.Contains(repo.Full_Name)) {
                    repositories.Remove(repo);
                }
                else {
                    Console.WriteLine($"[INFO]: Including: {repo.Full_Name}");
                }
            }

            // Exclude Filter:
            else if(_settings.Exclude != null && _settings.Exclude.Count > 0) {
                if(_settings.Exclude.Contains(repo.Full_Name)) {
                    repositories.Remove(repo);
                }
                else {
                    Console.WriteLine($"[INFO]: Including: {repo.Full_Name}");
                }
            }

            // No Filter:
            else {
                Console.WriteLine($"[INFO]: Including: {repo.Full_Name}");
            }
        }

        Console.WriteLine("[INFO]: Repository filtering complete.");
        return repositories;
    }

    static async Task LinkLabels()
    {
        
    }
}
