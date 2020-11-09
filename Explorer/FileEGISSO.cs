using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGISSOEditor
{
    public enum ActionOnFile
    {
        PatternCorrection, ErrorChecking
    }

    public class FileEGISSO
    {
        public string PrimaryDirectory { get; set; }
        public string TempDirectory { get; set; }
        public string FileName { get;}
        public bool isChangeFile { get; set; }

        public FileEGISSO(string primaryDirectory, string tempDirectory)
        {
            PrimaryDirectory = primaryDirectory;
            TempDirectory = tempDirectory;
            FileName = Path.GetFileName(primaryDirectory);
            
        }
    }
}
