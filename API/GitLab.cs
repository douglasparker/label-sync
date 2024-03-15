using System.Net.Http.Json;
using Configuration;

namespace API;

class GitLab
{
    public static async Task<Models.GitLab.Project> GetProject(string projectFullName, HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<Models.GitLab.Project>($"{settings.Url}/api/v1/repos/{projectFullName}");
    }
    public static async Task<List<Models.GitLab.Project>> GetProjects(HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<List<Models.GitLab.Project>>($"{settings.Url}/api/v4/projects?per_page=100");
    }
    public static async Task<List<Models.GitLab.Label>> GetProjectLabels(Models.GitLab.Project project, HttpClient httpClient, Settings settings)
    {
        return await httpClient.GetFromJsonAsync<List<Models.GitLab.Label>>($"{settings.Url}/api/v4/projects/{project.Id}/labels?per_page=100");
    }
    public static async Task<HttpResponseMessage> CreateProjectLabel(Models.GitLab.Project project, Models.GitLab.Label label, HttpClient httpClient, Settings settings)
    {
        return await httpClient.PostAsJsonAsync<Models.GitLab.Label>($"{settings.Url}/api/v4/projects/{project.Id}/labels", label);
    }
    public static async Task<HttpResponseMessage> UpdateProjectLabel(Models.GitLab.Project project, Models.GitLab.Label label, Int64 labelId, HttpClient httpClient, Settings settings)
    {
        return await httpClient.PutAsJsonAsync<Models.GitLab.Label>($"{settings.Url}/api/v4/projects/{project.Id}/labels/{labelId}", label);
    }
    public static async Task<HttpResponseMessage> DeleteProjectLabel(Models.GitLab.Project project, Int64 labelId, HttpClient httpClient, Settings settings)
    {
        return await httpClient.DeleteAsync($"{settings.Url}/api/v4/projects/{project.Id}/labels/{labelId}");
    }
    public static async Task PurgeProjectLabels(Models.GitLab.Project project, HttpClient httpClient, Settings settings)
    {
        foreach (Models.GitLab.Label label in await API.GitLab.GetProjectLabels(project, httpClient, settings))
        {
            await API.GitLab.DeleteProjectLabel(project, label.Id, httpClient, settings);
        }
    }
    public static async Task PurgeAllProjectLabels(List<Models.GitLab.Project> projects, HttpClient httpClient, Settings settings)
    {
        foreach (Models.GitLab.Project project in projects.OrderBy(o => o.Path_With_Namespace))
        {
            await API.GitLab.PurgeProjectLabels(project, httpClient, settings);
        }
    }
}
