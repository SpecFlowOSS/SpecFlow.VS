using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CommandLine;
using CommandLineParser = CommandLine.Parser;

namespace Deveroom.SampleProjectGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                CommandLineParser.Default.ParseArguments<GeneratorOptions>(args)
                    .WithParsed(RunOptionsAndReturnExitCode)
                    .WithNotParsed(HandleParseError);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1; 
            }
        }

        private static void RunOptionsAndReturnExitCode(GeneratorOptions opts)
        {
            var generator = opts.CreateProjectGenerator();
            generator.Generate();
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Errors");
            Console.WriteLine(string.Join(Environment.NewLine, errs));
        }
    }
}
