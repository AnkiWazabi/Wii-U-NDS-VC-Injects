using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Xml;

namespace nds_injects
{
    class Program
    {
        static void Main(string[] args)
        {
            var parentFolder = Directory.GetCurrentDirectory();
            var savedPath = "";
            var romPath = "";
            var baserom = "";
            var romname = "";
            var InjectName = "";
            var InjectName2 = "";
            var ReplacedPath = "";
            bool test = false;
            string[] lang = new string[12] { "ja", "en", "fr", "de", "it", "es", "zhs", "ko", "nl", "pt", "ru", "zht" };
            Directory.CreateDirectory("TEMP");
            var TEMP = Path.Combine(parentFolder, "TEMP");
            System.IO.DirectoryInfo di = new DirectoryInfo(TEMP);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
            Directory.CreateDirectory("Injects");
            var Injects = Path.Combine(parentFolder, "Injects");
            Console.WriteLine("Looking for Base Path");

            var BasePath = Path.Combine(parentFolder, "Base");
            if (Directory.Exists(BasePath))
            {
                savedPath = BasePath;
                Console.WriteLine("Base Folder Found");
            }
            else
            {
                Console.WriteLine("Base not Found, press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
            }
            Console.WriteLine("Enter file name (with .nds)");
            romPath = Console.ReadLine();
            /*
             * Console.WriteLine(romPath);
             * Console.WriteLine(savedPath);
            */
            Console.WriteLine("Copying Base content to TEMP folder");
            foreach (var subDirectory in Directory.GetDirectories(BasePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(subDirectory.Replace(BasePath, TEMP));
            }


            foreach (var file in Directory.GetFiles(BasePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(BasePath, TEMP), true);
            }
            Console.WriteLine("Adding the ROM to the Base");
            baserom = Path.GetFullPath(Path.Combine(TEMP, "content/0010"));
            ZipFile.ExtractToDirectory((Path.Combine(baserom, "rom.zip")), TEMP);
            romname = Path.GetFileNameWithoutExtension(Directory.GetFiles(TEMP, "*.srl")[0]);
            File.Copy(Path.Combine(".", romPath), Path.Combine(TEMP, (romname += ".srl")), true);
            File.Delete(Path.Combine(Path.Combine(TEMP, "content/0010"), "rom.zip"));
            using (var stream = new FileStream(Path.Combine(TEMP, "content", "0010", "rom.zip"), FileMode.Create))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
                archive.CreateEntryFromFile(TEMP + "\\" + romname, Path.GetFileName(romname));

            string[] directoryFiles = System.IO.Directory.GetFiles(TEMP, "*.srl");
            foreach (string directoryFile in directoryFiles)
            {
                System.IO.File.Delete(directoryFile);
            }
            Console.WriteLine("Enter your inject name");
            InjectName = Console.ReadLine();
            string metaXml = Path.Combine(TEMP, "meta", "meta.xml");
            string appXml = Path.Combine(TEMP, "code", "app.xml");
            Random random = new Random();
            string ID = $"{random.Next(0x3000, 0x10000):X4}{random.Next(0x3000, 0x10000):X4}";
            string ID2 = $"{random.Next(0x3000, 0x10000):X4}";
            XmlDocument doc = new XmlDocument();
            doc.Load(metaXml);
            int i = 0;
            for (int lg = 0; lg < lang.Length; lg++)
            {
                doc.SelectSingleNode($"menu/longname_{lang[lg]}").InnerText = InjectName.Replace(",", "\n");
                doc.SelectSingleNode($"menu/shortname_{lang[lg]}").InnerText = InjectName.Split(',')[0];
            }
            doc.SelectSingleNode("menu/drc_use").InnerText = "65537";
            doc.SelectSingleNode("menu/product_code").InnerText = $"WUP-N-{ID2}";
            doc.SelectSingleNode("menu/title_id").InnerText = $"00050002{ID}";
            doc.SelectSingleNode("menu/group_id").InnerText = $"0000{ID2}";
            doc.Save(metaXml);
            doc.Load(appXml);
            doc.SelectSingleNode("app/title_id").InnerText = $"00050002{ID}";
            doc.SelectSingleNode("app/group_id").InnerText = $"0000{ID2}";
            doc.Save(appXml);
            Console.WriteLine("Done");
            while (test == false)
            {
                Console.WriteLine("Do you want to pack the game ? [y/n]");
                string answer = Console.ReadLine();
                if (answer.ToLower() == "y")
                {
                    test = true;
                    Console.WriteLine("Enter your inject name");
                    InjectName2 = "[WUP] " + InjectName;
                    Directory.CreateDirectory(Injects + "//" + InjectName);
                    Console.WriteLine("Finishing");
                    ReplacedPath = Injects + "//" + InjectName;
                    if (Environment.Is64BitProcess)
                        CNUSPACKER.Program.Main(new string[] { "-in", TEMP, "-out", ReplacedPath, "-encryptKeyWith", "d7b00402659ba2abd2cb0db27fa2b656" });
                    else
                    {
                        using var cnuspacker = new Process();
                        cnuspacker.StartInfo.FileName = "java";
                        cnuspacker.StartInfo.Arguments = $"-jar NUSPacker.jar -in \"{TEMP}\" -out \"{ReplacedPath}\" -encryptKeyWith {"d7b00402659ba2abd2cb0db27fa2b656"}";
                        cnuspacker.Start();
                        cnuspacker.WaitForExit();
                    }
                    Console.WriteLine("Done. The Inject is in the Injects Folder.");
                }
                else if (answer.ToLower() == "n")
                {
                    ReplacedPath = Injects + "//" + InjectName;
                    foreach (var subDirectory in Directory.GetDirectories(TEMP, "*", SearchOption.AllDirectories))
                    {
                        Directory.CreateDirectory(subDirectory.Replace(TEMP, ReplacedPath));
                    }


                    foreach (var file in Directory.GetFiles(TEMP, "*.*", SearchOption.AllDirectories))
                    {
                        File.Copy(file, file.Replace(TEMP, ReplacedPath), true);
                    }
                    test = true;
                    Console.WriteLine("Done. The Inject is in the Injects Folder. Pack it with CNUSPACKER.");
                }

                else 
                {
                    Console.WriteLine("y or n");
                }

            }

            System.IO.DirectoryInfo clean = new DirectoryInfo(TEMP);
            foreach (FileInfo file in clean.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in clean.GetDirectories())
            {
                dir.Delete(true);
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
        }
      
    }
}