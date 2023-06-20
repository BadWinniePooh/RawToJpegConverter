using ImageMagick;
using Konsole;

namespace RawToJpeg;

public class Converter
{
    public Converter(string outputDir, Func<string, ProgressBar, int> func, ProgressBar pbLeft, int max = 0)
    {
        _outputDir = outputDir;
        _max = max;
        _func = func;
        _pbLeft = pbLeft;
    }

    private ProgressBar _pbLeft;
    private int _counter;
    private readonly string _outputDir;
    private int _max;
    private readonly Func<string,ProgressBar,int> _func;
    public List<Task> Tasks = new List<Task>();

    public int ProcessPath(string path, IConsole right)
    {
        if (isFile(path)) 
        {
            Tasks.Add(Task.Run(() =>
            {
                var pbRight = new ProgressBar(right, PbStyle.SingleLine, 4);
                _counter += _func(path, pbRight);
            }));
        }
        else if (isDirectory(path))
        {
            ProcessDirectory(path, right);
        }
        else
        {
            throw new ArgumentException("The given path is not a file or a directory.");
        }
        
        return _counter;
    }

    private void ProcessDirectory(string path, IConsole right)
    {
        var subs = Directory.GetDirectories(path).ToList();
        subs.AddRange(Directory.GetFiles(path));

        foreach (var subPath in subs)
        {
            ProcessPath(subPath, right);
        }
    }

    private bool isFile(string path) => File.Exists(path);
    private bool isDirectory(string path) => Directory.Exists(path);
    
    public int ProcessFile(string path, ProgressBar pb)
    {
        if (path == "")
        {
            return 0;
        }
        if (!Directory.Exists(_outputDir))
        {
            Directory.CreateDirectory(_outputDir);
        }
        var fileName = path.Split('\\').Last().Split('.')[0];
        pb.Refresh(0, $"{fileName}: Preparation to be done.");
        
        var outputPath = _outputDir + "\\" + fileName;
        var output = outputPath.Split('.')[0] + ".jpeg";
        
        using (var image = new MagickImage(path))
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
        UpdateProgressBar(_pbLeft,$"Tasks solved: {_counter + 1}/{_max}...");
        return 1;
    }

    private void UpdateProgressBar(ProgressBar pb, string output)
    {
        pb.Next(output);
    }
}