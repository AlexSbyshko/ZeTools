// See https://aka.ms/new-console-template for more information

using BinExtractor;

if (args.Length > 0)
{
    if (args[0] == "pack")
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Not enough args.");
        }
        
        var packer = new Packer();
        packer.Pack(args[1], args[2], args[3]);
    }
    else if (args[0] == "unpack")
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Not enough args.");
        }
        
        var unpacker = new Unpacker();
        unpacker.Unpack(args[1], args[2]);
    }
    else
    {
        Console.WriteLine("Not enough args.");
    }
}