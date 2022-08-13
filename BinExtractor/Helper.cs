namespace BinExtractor;

public static class Helper
{
    public static unsafe uint nonary_crypt(byte[] dataBytes, int size, uint key, uint relative_offset)
    {
        fixed (byte* data = dataBytes)
        {
            uint    eax,
                ecx,
                edx,
                edi,
                esi;

            eax = relative_offset;
            esi = (relative_offset + 3) << 0x18;
            ecx = (relative_offset + 2) << 0x10;
            edi = (relative_offset + 1) << 0x8;

            int i;
            for(i = 0; i < ((size / 4) * 4); i += 4) {
                edx = (ecx & 0xff0000) | (edi & 0xff00) | (esi & 0xff000000) | (eax & 0xff);
                eax += 0x4;
                edi += 0x400;
                ecx += 0x40000;
                esi += 0x4000000;
                *(int *)(data + i) ^= (int)(edx ^ key);
            }
            for(; i < size; i++) {
                data[i] ^= (byte)(eax ^ key);
                eax++;
                key >>= 8;
            }
            return eax;
        }
    }
    
    public static unsafe uint nonary_calculate_key(string name) {
        int     i,
            size;
        uint    eax = 0,
            esi = 0,
            edx = 0;

        //for(size = 0; name[size] != 0; size++);
        size = 13;

        for(i = 0; i < ((size / 2) * 2); i += 2) {
            eax += (uint)(name[i] + name[i+1]);
            edx = (uint)((name[i]   & 0xdf) + (esi * 0x83));
            esi = (uint)((name[i+1] & 0xdf) + (edx * 0x83));
        }
        for(; i < size; i++) {
            eax += name[i];
            esi = (uint)((name[i] & 0xdf)   + (esi * 0x83));
        }
        return (eax & 0xf) | ((esi & 0x07FFFFFF) << 4);
    }
    
    public static byte[] EncryptOrDecrypt(byte[] bytes, byte[] key)
    {
        byte[] xor = new byte[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            xor[i] = (byte)(bytes[i] ^ key[i % key.Length]);
        }
        return xor;
    }
    
    public static void Xor(byte[] bytes, byte[] key)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ key[i % key.Length]);
        }
    }
}