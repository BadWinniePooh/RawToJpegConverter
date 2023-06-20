// See https://aka.ms/new-console-template for more information

using Konsole;
using RawToJpeg;

//var ci = new ConvertImage();
//ci.Run();

Console.WriteLine("Please provide the output directory:");
var outputDir = Console.ReadLine();
Console.WriteLine("Please provide the input directory:");
var inputDir = Console.ReadLine();


// Prepare Progresswindow
var window = Window.OpenBox("Running Tasks");
var left = window.SplitLeft("Total progress");
var right = window.SplitRight("Current files");
var pbLeft = new ProgressBar(left, PbStyle.DoubleLine, 10);


//count files
pbLeft.Refresh(0, "Counting files...");

var c = new Converter(outputDir,(_,_) => 1, pbLeft);
var numberOfFiles = c.ProcessPath(inputDir, right);

//process files
pbLeft.Max = numberOfFiles + 1;
pbLeft.Refresh(0, "Processing files...");

c = new Converter(outputDir, c.ProcessFile, pbLeft, numberOfFiles + 1);
c.ProcessPath(inputDir, right);

Task.WaitAll(c.Tasks.ToArray());
