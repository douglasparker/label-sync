namespace Configuration.GitHub;

class Label
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
}

class LabelList
{
    public List<Label> Labels { get; set; }
}
