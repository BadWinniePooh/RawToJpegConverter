using ImageMagick;
using Konsole;

namespace RawToJpeg
{
    internal class ConvertImage
    {
        private readonly int _max = 4;
        private string _outputPath;
        public void Run()
        {
            Console.WriteLine("Please provide path with RAF's to convert:");
            var input = Console.ReadLine();
            
            //Console.Write("Performing some task... ");
            try
            {
                PathProcessor(input);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.ReadKey();
        }

        private Task PathProcessor(string path)
        {
            _outputPath = GetOutputPath(path);
            if (File.Exists(path))
            {
                var progress = new ProgressBar(10);

                // inputPath is a file
                progress.Refresh(5,path);
                var task = ProcessFile(path, new ProgressBar(PbStyle.SingleLine, _max));
                progress.Refresh(10, path);
                return task;
            } 
            if (Directory.Exists(path))
            {
                // inputPath is a directory
                return ProcessDirectory(path);
            }

            Console.WriteLine($"{path} is not a valid file or directory.");
            return Task.FromException(new Exception($"{path} is not a valid file or directory."));
        }

        private string GetOutputPath(string path)
        {
            var val = "";

            if (File.Exists(path))
            {
                var pathSegments = path.Split('\\').SkipLast(1);
                foreach (var pathSegment in pathSegments)
                {
                    val += pathSegment + '\\';
                }
                val += "\\Output";
            }

            if (Directory.Exists(path))
            {
                foreach (var pathChar in path)
                {
                    val += pathChar;
                }

                val += "\\Output";
            }

            return val;
        }

        private static void CreateDir(string inputPath)
        {
            if (!Directory.Exists(inputPath))
            {
                Directory.CreateDirectory(inputPath);
            }
        }

        private Task ProcessDirectory(string path)
        {
            var fileEntries = Directory.GetFiles(path);
            if (fileEntries.Length == 0)
            {
                try
                {
                    fileEntries = Directory.GetDirectories(path);
                    foreach (var entry in fileEntries)
                    {
                        if (entry.Contains("Output"))
                        {
                            continue;
                        }
                        ProcessDirectory(entry);
                    }
                    return Task.CompletedTask;
                }
                catch (Exception)
                {
                    throw new Exception("No files and no directories where found.");
                }
            }

            var tasks = new List<Task>();

            if (fileEntries.Length < 11)
            {
                for (var index = 0; index < fileEntries.Length; index++)
                {
                    var filePath = fileEntries[index];
                    if (File.Exists(filePath))
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            var pb = new ProgressBar(PbStyle.SingleLine, _max);
                            pb.Refresh(0, $"{GetFileName(filePath)}: Start Conversion.");
                            ProcessFile(filePath, pb);
                        }));
                    }
                    else if (Directory.Exists(filePath))
                    {
                        return ProcessDirectory(filePath);
                    }
                    else
                    {
                        throw new Exception("Something unexpected happened.");
                    }

                    
                }

                Task.WaitAll(tasks.ToArray());
            }
            else
            {
                var window = Window.OpenBox("Tasks");
                var left = window.SplitLeft("total");
                var right = window.SplitRight("files");

                var pb0 = new ProgressBar(left, PbStyle.SingleLine, 1000);
                pb0.Refresh(0, "Multiple Tasks.");

                var limit = 10;
                var lastDelimiter = 0;
                var whileVal = true;

                while (whileVal)
                {
                    tasks = new List<Task>();
                    for (var i = lastDelimiter; i < limit; i++)
                    {
                        var filePath = fileEntries[i];
                        if (File.Exists(filePath))
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                var pb1 = new ProgressBar(right, PbStyle.SingleLine, _max);
                                pb1.Refresh(0, $"{GetFileName(filePath)}: Start Conversion.");
                                ProcessFile(filePath, pb1);
                            }));

                            if (limit == fileEntries.Length)
                            {
                                whileVal = false;
                            }
                        } 
                        else if (Directory.Exists(filePath))
                        {
                            return ProcessDirectory(filePath);
                        }
                        else
                        {
                            throw new Exception("Something unexpected happened.");
                        }
                    }
                    
                    pb0.Refresh(GetMax(lastDelimiter, fileEntries), $"Running Tasks. {limit}/{fileEntries.Length}");
                    
                    lastDelimiter = limit;
                    limit += 10;
                    if (limit > fileEntries.Length)
                    {
                        limit = fileEntries.Length;
                    }

                    Task.WaitAll(tasks.ToArray());

                    pb0.Refresh(GetMax(limit, fileEntries), "Wait for next Tasks.");
                }
            }

            Console.WriteLine("All done!");
            return Task.CompletedTask;
        }

        private static int GetMax(int limit, string[] fileEntries)
        {
            var x =((double) limit / fileEntries.Length * 1000);
            return (int) x;
        }

        private Task ProcessFile(string inputPath, ProgressBar pb)
        {
            //var pathSegments = inputPath.Split('\\');
            //var outputGiven = _outputPath.Split('\\');

            //var outputPath = "";
            //for (var i = 0; i < pathSegments.Length; i++)
            //{
            //    if (i < outputGiven.Length)
            //    {
            //        outputPath += outputGiven[i] + '\\';
            //    }
            //    else if (i == outputGiven.Length)
            //    {
            //        i--;
            //    }
            //    else
            //    {
            //        outputPath += pathSegments[i] + @"\";
            //    }
            //}

            //outputPath = outputPath.TrimEnd('\\');

            CreateDir(_outputPath);
            var fileName = GetFileName(inputPath);
            
            var outputPath = _outputPath + "\\" + fileName;
            var output = outputPath.Split('.')[0] + ".jpeg";
            

            using (var image = new MagickImage(inputPath))
            {
                pb.Refresh(1, $"{fileName}: Write file to jpeg.");

                //image.Write(output, MagickFormat.Png);
                image.Write(output, MagickFormat.Jpeg);

                pb.Refresh(2, $"{fileName}: Begin optimizing.");

                var optimizer = new ImageOptimizer();

                pb.Refresh(3, $"{fileName}: Lossless compression.");

                optimizer.LosslessCompress(output);

                pb.Refresh(4, $"{fileName}: Finished Conversion.");
            }
            return Task.CompletedTask;
        }

        private static string GetFileName(string path)
        {
            var fileName = path.Split('\\').Last().Split('.')[0];
            return fileName;
        }
    }
}
