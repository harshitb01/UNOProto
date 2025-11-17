using UnityEngine;

public static class RoomCodeGenerator
{
    static readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Generate(int length = 6)
    {
        char[] code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = chars[Random.Range(0, chars.Length)];
        }
        return new string(code);
    }
}
