using System;

namespace AttributeTest;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; }
    public string Description { get; set; }

    public CommandAttribute(string name)
    {
        Name = name;
        Description = "";
    }
}
