using ResourceUtilityLib;

foreach (var arg in args)
{
    Console.WriteLine("Processing " + arg);
    ResourceUtility ru = new ResourceUtility(arg);

    Console.WriteLine("Version: " + ru.FileVersion());
    Console.WriteLine("Resources: " + ru.Count());
    foreach (string str in ru.ListContents(true))
    {
        Console.WriteLine(str);
    }
}
