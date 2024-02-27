namespace Models.Forgejo;

class Label
{
    public Int64 Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
    public string Url { get; set; }
    public bool Exclusive { get; set; }
    public bool Is_Archived { get; set; }
}
