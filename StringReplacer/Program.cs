// See https://aka.ms/new-console-template for more information

using System.Text;

if (args.Length < 3)
{
    Console.WriteLine("Not enough args.");
    return;
}

var filePath = args[0];
var sourceString = args[1];
var destString = args[2];

var fileBytes = File.ReadAllBytes(filePath);
var sourceStringBytes = Encoding.UTF8.GetBytes(sourceString);
var sizeBytes = BitConverter.GetBytes(sourceStringBytes.Length + 1);
var sourceSequence = sizeBytes.Concat(sourceStringBytes).ToArray();

Console.WriteLine($"Searching string '{sourceString}'...");
var sizeOffset = FindSequence(fileBytes, sourceSequence);
if (sizeOffset == -1)
{
    Console.WriteLine("String not found.");
    return;
}

Console.WriteLine($"String found. Replacing with '{destString}'");
var endStringOffset = sizeOffset + 4 + sourceStringBytes.Length;

var beginBytes = fileBytes[..sizeOffset];
var endBytes = fileBytes[endStringOffset..];

var destStringBytes = Encoding.UTF8.GetBytes(destString);

var destFileSize = beginBytes.Length + 4 + destStringBytes.Length + endBytes.Length;
var resultBytes = new byte[destFileSize];

using var writer = new BinaryWriter(new MemoryStream(resultBytes));

writer.Write(beginBytes);
writer.Write(destStringBytes.Length + 1);
writer.Write(destStringBytes);
writer.Write(endBytes);

File.WriteAllBytes(filePath, resultBytes);

Console.WriteLine("Done.");


int FindSequence(byte[] source, byte[] seq)
{
    var start = -1;
    for (var i = 0; i < source.Length - seq.Length + 1 && start == -1; i++)
    {
        var j = 0;
        for (; j < seq.Length && source[i+j] == seq[j]; j++) {}
        if (j == seq.Length) start = i;
    }
    return start;
}