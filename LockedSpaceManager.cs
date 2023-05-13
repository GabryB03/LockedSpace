using System.Collections.Generic;
using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Text;

public class LockedSpaceManager
{
    private static string[] _directories = new string[] { "decrypted_space", "output_spaces", "space_to_crypt" };

    public static void CreateLockedSpace(string spaceName, string password)
    {
        FixPaths();

        if (spaceName.Replace(" ", "").Replace('\t'.ToString(), "") == "")
        {
            MessageBox.Show("The locked space name is empty.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (File.Exists($"output_spaces\\{spaceName}.lockedspace"))
        {
            MessageBox.Show("The specified locked space name already exists in the folder 'output_spaces'.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (password.Replace(" ", "").Replace('\t'.ToString(), "") == "")
        {
            MessageBox.Show("The password is empty.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string[] paths = FindFiles("space_to_crypt").ToArray();

        if (paths.Length == 0)
        {
            MessageBox.Show("No files/folders to include in the space are available in the 'space_to_crypt' folder.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        byte[] fullBytes = new byte[] { };

        foreach (string path in paths)
        {
            string thePath = path.Substring("space_to_crypt\\".Length);
            byte[] pathBytes = Encoding.Unicode.GetBytes(thePath);

            if (File.Exists(path))
            {
                byte[] fileBytes = File.ReadAllBytes(path);
                fullBytes = Combine(fullBytes, new byte[1] { 0x02 },
                    BitConverter.GetBytes(pathBytes.Length), pathBytes,
                    BitConverter.GetBytes(fileBytes.Length), fileBytes);
            }
            else
            {
                fullBytes = Combine(fullBytes, new byte[1] { 0x01 },
                    BitConverter.GetBytes(pathBytes.Length), pathBytes);
            }
        }

        File.WriteAllBytes($"output_spaces\\{spaceName}.lockedspace", 
            SecureAES.SecureAES.Encrypt(fullBytes, Encoding.Unicode.GetBytes(password)));

        MessageBox.Show("Succesfully created your locked space! The output locked space is in the folder 'output_spaces'.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void DecryptLockedSpace(string spacePath, string password)
    {
        FixPaths();

        if (!File.Exists(spacePath))
        {
            MessageBox.Show("The specified locked space path does not exist.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!Path.GetExtension(spacePath).Equals(".lockedspace"))
        {
            MessageBox.Show("The extension of the specified locked space path is not valid.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (password.Replace(" ", "").Replace('\t'.ToString(), "") == "")
        {
            MessageBox.Show("The password is empty.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Directory.Delete("decrypted_space", true);
        Directory.CreateDirectory("decrypted_space");

        byte[] decryptedBytes = SecureAES.SecureAES.Decrypt(File.ReadAllBytes(spacePath), Encoding.Unicode.GetBytes(password));

        while (decryptedBytes.Length > 0)
        {
            if (decryptedBytes[0] == 0x01)
            {
                decryptedBytes = decryptedBytes.Skip(1).ToArray();

                int pathLength = BitConverter.ToInt32(decryptedBytes.Take(4).ToArray(), 0);
                decryptedBytes = decryptedBytes.Skip(4).ToArray();

                byte[] pathBytes = decryptedBytes.Take(pathLength).ToArray();
                decryptedBytes = decryptedBytes.Skip(pathLength).ToArray();
                string path = Encoding.Unicode.GetString(pathBytes);

                Directory.CreateDirectory($"decrypted_space\\{path}");
            }
            else if (decryptedBytes[0] == 0x02)
            {
                decryptedBytes = decryptedBytes.Skip(1).ToArray();

                int pathLength = BitConverter.ToInt32(decryptedBytes.Take(4).ToArray(), 0);
                decryptedBytes = decryptedBytes.Skip(4).ToArray();

                byte[] pathBytes = decryptedBytes.Take(pathLength).ToArray();
                decryptedBytes = decryptedBytes.Skip(pathLength).ToArray();
                string path = Encoding.Unicode.GetString(pathBytes);

                int fileLength = BitConverter.ToInt32(decryptedBytes.Take(4).ToArray(), 0);
                decryptedBytes = decryptedBytes.Skip(4).ToArray();

                byte[] fileBytes = decryptedBytes.Take(fileLength).ToArray();
                File.WriteAllBytes($"decrypted_space\\{path}", fileBytes);

                decryptedBytes = decryptedBytes.Skip(fileLength).ToArray();
            }
        }

        MessageBox.Show("Succesfully decrypted your locked space! The output files/folders are in the folder 'decrypted_space'.", "LockedSpace", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static List<string> FindFiles(string path)
    {
        List<string> paths = new List<string>();

        foreach (string filePath in Directory.GetFiles(path))
        {
            paths.Add(filePath);
        }

        foreach (string folderPath in Directory.GetDirectories(path))
        {
            paths.Add(folderPath);
            paths.AddRange(FindFiles(folderPath));
        }

        return paths;
    }

    private static void FixPaths()
    {
        foreach (string directory in _directories)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        byte[] ret = new byte[arrays.Sum(x => x.Length)];
        int offset = 0;

        foreach (byte[] data in arrays)
        {
            Buffer.BlockCopy(data, 0, ret, offset, data.Length);
            offset += data.Length;
        }

        return ret;
    }
}