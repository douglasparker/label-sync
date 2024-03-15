namespace Models.GitLab;

class Label
{
    public Int64 Id { get; set; }
    public string Name { get; set; }
    public string New_Name { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public int? Priority { get; set; }
}
