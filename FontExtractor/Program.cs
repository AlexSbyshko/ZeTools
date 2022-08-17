// See https://aka.ms/new-console-template for more information

using System.Text;
using FontExtractor;

if (args.Length < 3)
{
    Console.WriteLine("not enough parameters");
    return;
}

if (args[0] == "unpack")
{
    FontUnpacker.Unpack(args[1], args[2]);
}
else if (args[0] == "pack")
{
    FontUnpacker.Pack(args[1], args[2]);
}
else
{
    Console.WriteLine("first parameter should be pack or unpack");
}