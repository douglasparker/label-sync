using System.Net.Http.Json;
using Configuration;

namespace API;

class GitHub
{
    public static async Task<Models.GitHub.Repository> GetRepository(string repositoryFullName, HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<Models.GitHub.Repository>($"{settings.Url}/repos/{repositoryFullName}");
    }
    public static async Task<List<Models.GitHub.Repository>> GetRepositories(HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<List<Models.GitHub.Repository>>($"{settings.Url}/user/repos?per_page=100");
    }
    public static async Task<List<Models.GitHub.Label>> GetRepositoryLabels(Models.GitHub.Repository repository, HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<List<Models.GitHub.Label>>($"{settings.Url}/repos/{repository.Full_Name}/labels?per_page=100");
    }
    public static async Task<HttpResponseMessage> CreateRepositoryLabel(Models.GitHub.Repository repository, Models.GitHub.Label label, HttpClient httpClient, Settings settings)
    {
        return await httpClient.PostAsJsonAsync<Models.GitHub.Label>($"{settings.Url}/repos/{repository.Full_Name}/labels", label);
    }
    public static async Task<HttpResponseMessage> UpdateRepositoryLabel(Models.GitHub.Repository repository, Models.GitHub.Label label, string labelName, HttpClient httpClient, Settings settings)
    {
        return await httpClient.PatchAsJsonAsync<Models.GitHub.Label>($"{settings.Url}/repos/{repository.Full_Name}/labels/{labelName}", label);
    }
    public static async Task<HttpResponseMessage> DeleteRepositoryLabel(Models.GitHub.Repository repository, string labelName, HttpClient httpClient, Settings settings)
    {
        return await httpClient.DeleteAsync($"{settings.Url}/repos/{repository.Full_Name}/labels/{labelName}");
    }
    public static async Task PurgeRepositoryLabels(Models.GitHub.Repository repository, HttpClient httpClient, Settings settings)
    {
        foreach (Models.GitHub.Label label in await API.GitHub.GetRepositoryLabels(repository, httpClient, settings))
        {
            await API.GitHub.DeleteRepositoryLabel(repository, label.Name, httpClient, settings);
        }
    }
    public static async Task PurgeAllRepositoryLabels(List<Models.GitHub.Repository> repositories, HttpClient httpClient, Settings settings)
    {
        foreach (Models.GitHub.Repository repository in repositories.OrderBy(o => o.Full_Name))
        {
            await API.GitHub.PurgeRepositoryLabels(repository, httpClient, settings);
        }
    }
}
