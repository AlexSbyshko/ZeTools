namespace BinExtractor;

public class FileReader
{
    private readonly List<byte[]> _parts = new();

    public void Read(string fileName)
    {
        int bufferSize = 1000 * 1024 * 1024; // 1000MB
        byte[] buffer = new byte[bufferSize];
        int bytesRead = 0;

        using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        
        while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
        {
            if (bytesRead < bufferSize)
            {
                //result.AddRange(buffer[..bytesRead]);
                // please note array contains only 'bytesRead' bytes from 'bufferSize'
            }

            // here 'buffer' you get current portion on file 
            // process this
            _parts.Add(buffer[..bytesRead]);
        }
    }
}