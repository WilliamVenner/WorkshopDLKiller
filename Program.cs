using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace WorkshopDLKiller {
    class Program {

        private static string ReadString(BinaryReader reader) {
            string result = string.Empty;

            while (true)
            {
                char Char = reader.ReadChar();

                if (Char == '\0')
                    break;

                result += Char;
            }

            return result;
        }

        public static string ScrubFileName(string value) {
            StringBuilder sb = new StringBuilder(value);
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char item in invalid)
            {
                sb.Replace(item.ToString(), "");
            }
            return sb.ToString();
        }

        public static void Extract(string gmaFile, string folderpath, string output=null) {
            FileStream fs = new FileStream(gmaFile, FileMode.Open);
            BinaryReader reader = new BinaryReader(fs, Encoding.GetEncoding(1252));

            uint i = reader.ReadUInt32();

            if (i != 0x44414d47)
            {
                reader.Close();
                fs.Close();
                return;
            }

            fs.Seek(18, SeekOrigin.Current);

            string addonName = ReadString(reader);
            string addonDesc = ReadString(reader);
            string addonAuthor = ReadString(reader);

            fs.Seek(4, SeekOrigin.Current);

            List<object[]> compressedFiles = new List<object[]>();

            while (true)
            {
                uint filenum = reader.ReadUInt32();
                if (filenum == 0)
                    break;

                object[] file = new object[2];
                file[0] = ReadString(reader);
                file[1] = reader.ReadUInt32();

                compressedFiles.Add(file);
                fs.Seek(8, SeekOrigin.Current);
            }

            if (compressedFiles.Count < 1)
            {
                reader.Close();
                fs.Close();
                return;
            }

            Regex exp = new Regex("[^a-zA-Z0-9_]");
            Regex exp2 = new Regex("_+");
            string folderName = "workshop-" + Path.GetFileNameWithoutExtension(gmaFile).Substring(3) + "-" + exp2.Replace(exp.Replace(ScrubFileName(addonName).ToLower().Replace(" ", "_"), ""), "_");
            string addonDir = "";
            if (output != null) {
                addonDir = Path.Combine(output, folderName);
            } else {
                addonDir = Path.Combine(folderpath, folderName);
            }
            if (!Directory.Exists(addonDir))
                Directory.CreateDirectory(addonDir);

            foreach (object[] file in compressedFiles)
            {
                byte[] fileContent = reader.ReadBytes(Convert.ToInt32(file[1]));
                string fileDir = Path.Combine(addonDir, Path.GetDirectoryName((string)file[0]));
                string fileName = Path.GetFileName((string)file[0]);

                if (!Directory.Exists(fileDir))
                    Directory.CreateDirectory(fileDir);

                File.WriteAllBytes(Path.Combine(fileDir, fileName), fileContent);
            }

            reader.Close();
            fs.Close();
            
            Console.WriteLine("|| Extracted \"" + addonName + "\" to \"" + folderName + "\"");
        }

        public static List<string> GetGMAFilesFromPath(string path) {
            string[] files = Directory.GetFiles(path);
            List<string> gmafiles = new List<string>();
            foreach (string file in files)
            {
                FileStream OpenFile = File.Open(file, FileMode.Open);
                if (Path.GetExtension(OpenFile.Name) == ".gma")
                {
                    gmafiles.Add(OpenFile.Name);
                }
                OpenFile.Close();
            }
            return gmafiles;
        }

        [STAThread]
        static void Main(string[] args) {
            if (args.Count() > 0) {
                Dictionary<string, string> betterargs = new Dictionary<string, string>();
                bool mode = true;
                string last = "";
                foreach(string arg in args) {
                    if (mode) {
                        mode = false;
                        last = arg;
                    } else {
                        mode = true;
                        betterargs[last] = arg;
                    }
                }
                if (betterargs.ContainsKey("-folder") && betterargs.ContainsKey("-output")) {
                    if (!Directory.Exists(betterargs["-folder"])) {
                        Console.WriteLine("-folder not found!");
                        return;
                    }
                    if (!Directory.Exists(betterargs["-output"])) {
                        Console.WriteLine("-output directory not found!");
                        return;
                    }
                    foreach (string gmaFile in GetGMAFilesFromPath(betterargs["-folder"])) {
                        Extract(gmaFile, betterargs["-folder"], betterargs["-output"]);
                    }
                } else if (betterargs.ContainsKey("-folder")) {
                    if (!Directory.Exists(betterargs["-folder"])) {
                        Console.WriteLine("-folder not found!");
                        return;
                    }
                    foreach (string gmaFile in GetGMAFilesFromPath(betterargs["-folder"]))
                    {
                        Extract(gmaFile, betterargs["-folder"]);
                    }
                } else if (betterargs.ContainsKey("-output")) {
                    Console.WriteLine("You need to supply -folder.");
                    return;
                }
                return;
            }
            Console.Title = "WorkshopDLKiller";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("//------------------------------------------//");
            Console.WriteLine("||             WorkshopDLKiller             ||");
            Console.WriteLine("//------------------------------------------//");
            Console.WriteLine("|| Please select your addons folder (note: -folder for commandline usage)");
            FolderBrowserDialog FileDialog = new FolderBrowserDialog();
            DialogResult result = FileDialog.ShowDialog();
            if (result == DialogResult.OK) {
                string[] files = Directory.GetFiles(FileDialog.SelectedPath);
                List<string> gmafiles = new List<string>();
                foreach(string file in files) {
                    FileStream OpenFile = File.Open(file,FileMode.Open);
                    if (Path.GetExtension(OpenFile.Name) == ".gma") {
                        gmafiles.Add(OpenFile.Name);
                    }
                    OpenFile.Close();
                }
                Console.Write("|| Do you want to extract into a different folder? (-output) y/n");
                if (Console.ReadKey(true).KeyChar.ToString().ToLower() == "y") {
                    Console.Write("\n");
                    Console.WriteLine("|| Where do you want to extract to?");
                    FolderBrowserDialog OutputDialog = new FolderBrowserDialog();
                    DialogResult outputresult = OutputDialog.ShowDialog();
                    if (outputresult == DialogResult.OK) {
                        foreach (string gmaFile in gmafiles) {
                            Extract(gmaFile, FileDialog.SelectedPath, OutputDialog.SelectedPath);
                        }
                    } else {
                        foreach (string gmaFile in gmafiles) {
                            Extract(gmaFile, FileDialog.SelectedPath);
                        }
                    }
                } else {
                    Console.Write("\n");
                    foreach (string gmaFile in gmafiles) {
                        Extract(gmaFile, FileDialog.SelectedPath);
                    }
                }
                Console.WriteLine("|| Extracted all found .gmas into the folder you specified.");
            } else {
                return;
            }
            Console.WriteLine("|| Press any key to close.");
            Console.Write("|| ");
            Console.ReadKey(true);
            return;
        }
    }
}