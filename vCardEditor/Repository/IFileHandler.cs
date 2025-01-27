﻿namespace vCardEditor.Repository
{
    public interface IFileHandler
    {
        void MoveFile(string newFilename, string oldFilename);
        bool FileExist(string filename);
        string[] ReadAllLines(string filename);
        void WriteAllText(string fileName, string contents);
        string GetExtension(string path);
        void WriteBytesToFile(string imageFile, byte[] image);
    }
}
