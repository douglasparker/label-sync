namespace Configuration;

class Settings
{
    public string Url { get; set; }
    public string Username { get; set; }
    public List<string> Include { get; set; }
    public List<string> Exclude { get; set; }
    public string ApiKey { get; set; }
    public string LogLevel { get; set; }
}
