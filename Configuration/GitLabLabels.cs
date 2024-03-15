namespace Configuration.GitLab;

class Label
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public int? Priority { get; set; }
}

class LabelList
{
    public List<Label> Labels { get; set; }
}
