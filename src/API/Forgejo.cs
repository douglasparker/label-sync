using System.Net.Http.Json;
using Configuration;
using Microsoft.Extensions.Configuration;
using Models.ForgejoAPI;

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
    public static void GetRepositoryLabels()
    {

    }
    public static void CreateRepositoryLabel()
    {

    }
    public static void UpdateRepositoryLabel()
    {

    }
    public static void DeleteRepositoryLabel()
    {

    }
    public static void PurgeRepositoryLabels()
    {

    }
    public static void PurgeAllRepositoryLabels()
    {

    }
}
