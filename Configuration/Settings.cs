namespace Configuration;

class Settings
{
    public int Forge { get; set; }
    public string Url { get; set; }
    public string Username { get; set; }
    public string ApiKey { get; set; }
    public List<string> Include { get; set; }
    public List<string> Exclude { get; set; }
    public bool PurgeUndefinedLabels { get; set; }
    public string LogLevel { get; set; }
}
