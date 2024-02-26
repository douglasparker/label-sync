namespace Configuration;

class Label
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public bool Exclusive { get; set; }
    public bool Archived { get; set; }
}

class LabelList
{
    public List<Label> Labels { get; set; }
}
