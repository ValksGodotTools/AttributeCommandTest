using Godot;

namespace AttributeTest;

public class Foo
{
    [Command("Test")]
    private void Test(int x)
    {
        GD.Print($"There are {x} sheep");
    }

    [Command("Test2")]
    public void Test2()
    {
        GD.Print("Bob says hello.");
    }
}
