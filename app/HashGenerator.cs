using System.Security.Cryptography;
using System.Text;

namespace TTSRehoster;

public static class HashGenerator
{
    public static string Md5(string input)
    {
        // Create an MD5 hash object 
        using MD5 md5 = MD5.Create();
        
        // Compute the hash from the input string 
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        // Convert the byte array to a hexadecimal string 
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2")); // "x2" formats the byte as a two-digit hexadecimal 
        }

        return sb.ToString();
    }
}