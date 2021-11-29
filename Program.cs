using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace MillPlane
{
    class Program
    {
        static int Main(string[] args)
        {
            var optionToolDiameter = new Option<double>("--tool-diameter", description: "Diameter of the tool")
            {
                IsRequired = true
            };

            var optionDepth = new Option<double>("--depth", description: "Depth to remove")
            {
                IsRequired = true
            };

            var optionOutput = new Option<FileInfo>("--output", "Output filename")
            {
                IsRequired = true
            };

            var optionRevolutionsPerMinute = new Option<int>("--rpm", description: "RPM", getDefaultValue: () => 10000);
            var optionFeedRate = new Option<double>("--feed", description: "Feed rate", getDefaultValue: () => 30.0);
            var optionStepDown = new Option<double>("--step-down", description: "Stepdown");
            var optionStepOver = new Option<double>("--step-over", description: "Stepover");
            var optionWidth = new Option<double>("--width", description: "Width of stock");
            var optionHeight = new Option<double>("--height", description: "Height of stock");

            var rootCommand = new RootCommand
            {
                optionToolDiameter,
                optionRevolutionsPerMinute,
                optionFeedRate,
                optionStepOver,
                optionStepDown,
                optionWidth,
                optionHeight,
                optionDepth,
                optionOutput
            };

            rootCommand.Description = "Utility for generating gcode for plane milling stock";
            rootCommand.Handler = CommandHandler.Create<double, int, double, double, double, double, double, double, FileInfo>(Build);

            // Parse the incoming args and invoke the handler
            try
            {
                return rootCommand.Invoke(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException?.Message);
            }

            return 1;
        }

        private static void Build(double toolDiameter, int rpm, double feed, double stepOver, double stepDown, double width, double height, double depth, FileInfo output)
        {
            if (width < toolDiameter) width = toolDiameter;
            if (height < toolDiameter) height = toolDiameter;
            if (depth > 0) depth = 0 - depth;
            if (stepOver <= 0) stepOver = toolDiameter * 0.4;
            if (stepDown <= 0) stepDown = toolDiameter * 0.1;

            var toolRadius = (toolDiameter / 2);
            var startX = (0 - toolRadius);
            var endX = (width + toolRadius);

            using var stream = output.Open(FileMode.Create);
            using var writer = new StreamWriter(stream);

            writer.Write("%\n");
            // Set units to inches
            writer.Write("G20\n");
            // Switch to absolute distance mode
            writer.Write("G90\n");
            // Move to origin
            writer.Write("G0X0.000Y0.000Z0.125\n");

            // Spindle off
            writer.Write("M5\n");
            // Tool comment
            writer.Write($"(TOOL/MILL,{toolDiameter:0.000}, 0.000, 0.000, 0.00)\n");
            // Change tool to tool 1
            writer.Write("M6T1\n");
            // Spindle on
            writer.Write($"M03S{rpm}\n");
            // Move tool to initial cut origin)
            writer.Write($"G0X{Math.Round(startX, 4)}Y0.000\n");
            writer.Write("G0Z0.125\n");

            var currentHeight = 0.0;
            var currentDepth = 0.0;

            while (currentDepth > depth)
            {
                // Step down
                currentDepth -= stepDown;

                // Ensure step down does not exceed final depth
                if (currentDepth < depth)
                {
                    currentDepth = depth;
                }

                while (currentHeight <= height)
                {
                    // Plunge Z to starting cut depth
                    writer.Write($"G1Z{Math.Round(currentDepth, 4)}F10.0\n");

                    // Cutting pass
                    writer.Write($"G1X{Math.Round(endX, 4)}F{feed:0.0##}\n");

                    // Retract and return to start of pass
                    writer.Write($"G0X{Math.Round(startX, 4)}Y{Math.Round(currentHeight, 4)}Z0.125\n");

                    // Increment height
                    currentHeight += stepOver;

                    // Rapid to start of pass
                    writer.Write($"G0X{Math.Round(startX, 4)}Y{Math.Round(currentHeight, 4)}Z0.125\n");
                }
            }

            // Retract Z
            writer.Write("G0Z0.250\n");

            // Spindle off
            writer.Write("M5\n");

            // End program
            writer.Write("M30\n");
            writer.Write("(END)\n");
            writer.Flush();
            writer.Close();
        }
    }
}
