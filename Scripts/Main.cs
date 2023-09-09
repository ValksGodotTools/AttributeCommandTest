using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AttributeTest;

/*
 * Some code from https://github.com/LauraWebdev/SofiaConsole 
 * (by Laura Sofia Heimann) was directly used here. Go check them out 
 * and star their repository!
 */
public partial class Main : Node
{
    readonly List<CommandInfo> commands = new();

    VBoxContainer list; // all console commands displayed here
    LineEdit input; // console commands are inputted here

    public override void _Ready()
    {
        input = GetNode<LineEdit>("%Input");
        list = GetNode<VBoxContainer>("%List");

        LoadCommands();

        input.TextSubmitted += ProcessCommand;
    }

    void LoadCommands()
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();

        foreach (Type type in types)
        {
            // BindingFlags.Instance must be added or the methods will not
            // be seen. Not sure why static methods can't be added.
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                object[] attributes =
                    method.GetCustomAttributes(
                        attributeType: typeof(CommandAttribute),
                        inherit: false);

                foreach (object attribute in attributes)
                {
                    if (attribute is not CommandAttribute cmd)
                        continue;

                    TryLoadCommand(cmd, method);
                }
            }
        }
    }

    void TryLoadCommand(CommandAttribute cmd, MethodInfo method)
    {
        if (commands.FirstOrDefault(x => x.Name == cmd.Name) != null)
        {
            throw new Exception($"Duplicate console command: {cmd.Name}");
        }

        commands.Add(new CommandInfo
        {
            Name = cmd.Name,
            Method = method
        });

        list.AddChild(new Label
        {
            Text = cmd.Name
        });
    }

    void ProcessCommand(string text)
    {
        CommandInfo cmd = commands.Find(cmd => cmd.Name.ToLower() == text);
        
        if (cmd == null)
        {
            GD.Print("Command does not exist");
            return;
        }
        
        MethodInfo method = cmd.Method;

        // Not sure why this has to be the "DeclaringType" as suppose to
        // any of the other Type properties or methods
        object instance = GetMethodInstance(cmd.Method.DeclaringType);

        // Valk: Not really sure what this regex is doing. May rewrite
        // code in a more readable fassion.

        // Split by spaces, unless in quotes
        string[] rawCommandSplit = Regex.Matches(text,
            @"[^\s""']+|""([^""]*)""|'([^']*)'").Select(m => m.Value)
            .ToArray();

        object[] parameters = ConvertMethodParams(method, rawCommandSplit);

        method.Invoke(instance, parameters);
    }

    object[] ConvertMethodParams(MethodInfo method, string[] rawCmdSplit)
    {
        ParameterInfo[] paramInfos = method.GetParameters();
        object[] parameters = new object[paramInfos.Length];
        for (int i = 0; i < paramInfos.Length; i++)
        {
            if (rawCmdSplit.Length > i + 1 && rawCmdSplit[i + 1] != null)
            {
                parameters[i] = ConvertStringToType(
                    input: rawCmdSplit[i + 1],
                    targetType: paramInfos[i].ParameterType);
            }
            else
            {
                parameters[i] = null;
            }
        }

        return parameters;
    }

    object GetMethodInstance(Type type)
    {
        object instance;

        if (type.IsSubclassOf(typeof(GodotObject)))
        {
            // This is a Godot Object, find it or create a new instance
            instance = FindNodeByType(GetTree().Root, type) ??
                Activator.CreateInstance(type);
        }
        else
        {
            // This is a generic class, create a new instance
            instance = Activator.CreateInstance(type);
        }

        return instance;
    }

    object ConvertStringToType(string input, Type targetType)
    {
        if (targetType == typeof(string))
            return input;

        if (targetType == typeof(int))
            return int.Parse(input);

        if (targetType == typeof(float))
        {
            // Valk: Not entirely sure what is happening here other than
            // convert the input to a float.
            float.TryParse(input.Replace(',', '.'),
                style: NumberStyles.Any,
                provider: CultureInfo.InvariantCulture,
                result: out var value);

            return value;
        }

        if (targetType == typeof(bool))
            return bool.Parse(input);

        throw new ArgumentException($"Unsupported type: {targetType}");
    }

    // Valk: I have not tested this code to see if it works with 100%
    // no errors.
    Node FindNodeByType(Node root, Type targetType)
    {
        if (root.GetType() == targetType)
            return root;

        foreach (Node child in root.GetChildren())
        {
            Node foundNode = FindNodeByType(child, targetType);

            if (foundNode != null)
                return foundNode;
        }

        return null;
    }
}
