using System.Net.Http.Json;
using Configuration;
using Models.Forgejo;

namespace API;

/// <summary>
/// Class <c>Forgejo</c> provides an interface to perform RESTful actions against a Forgejo REST API.
/// </summary>
class Forgejo
{
    /// <summary>
    /// Get a repository object from a Forgejo REST API.
    /// </summary>
    /// <returns>
    /// Returns a Task Repository object
    /// </returns>
    public static async Task<Repository> GetRepository(string repositoryFullName, HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<Repository>($"{settings.Url}/api/v1/repos/{repositoryFullName}");
    }
    public static async Task<List<Repository>> GetRepositories(HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<List<Repository>>($"{settings.Url}/api/v1/user/repos");
    }
    public static async Task<List<Models.Forgejo.Label>> GetRepositoryLabels(Repository repository, HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<List<Models.Forgejo.Label>>($"{settings.Url}/api/v1/repos/{repository.Full_Name}/labels");
    }
    public static async Task<HttpResponseMessage> CreateRepositoryLabel(Repository repository, Models.Forgejo.Label label, HttpClient httpClient, Settings settings)
    {
        return await httpClient.PostAsJsonAsync<Models.Forgejo.Label>($"{settings.Url}/api/v1/repos/{repository.Full_Name}/labels", label);
    }
    public static async Task<HttpResponseMessage> UpdateRepositoryLabel(Repository repository, Models.Forgejo.Label label, Int64 labelId, HttpClient httpClient, Settings settings)
    {
        return await httpClient.PatchAsJsonAsync<Models.Forgejo.Label>($"{settings.Url}/api/v1/repos/{repository.Full_Name}/labels/${labelId}", label);
    }
    public static async Task<Models.Forgejo.Label> DeleteRepositoryLabel(Repository repository, Int64 labelId, HttpClient httpClient, Settings settings)
    {
        return await httpClient.DeleteFromJsonAsync<Models.Forgejo.Label>($"{settings.Url}/api/v1/repos/{repository.Full_Name}/labels/${labelId}");
    }
    public static void PurgeRepositoryLabels()
    {

    }
    public static void PurgeAllRepositoryLabels()
    {

    }
}
