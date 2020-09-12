using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ErikTheCoder.Utilities;


namespace ErikTheCoder.ProgrammingPearls.PhoneNumberSort
{
    public static class Program
    {
        private const int _exclusiveMaxPhoneNumber = 10_000_000;
        private const int _bitsPerInt = sizeof(int) * 8;
        private const string _elapsedSecondsFormat = "0.000";
        private const string _phoneNumberFormat = "000-0000";
        
        
        public static async Task Main(string[] Arguments)
        {
            try
            {
                await Run(Arguments);
                ThreadsafeConsole.WriteLine(null);
            }
            catch (Exception exception)
            {
                ThreadsafeConsole.WriteLine(exception.GetSummary(true, true), ConsoleColor.Red);
            }
        }


        private static async Task Run(IReadOnlyList<string> Arguments)
        {
            var stopwatch = Stopwatch.StartNew();
            var (phoneNumberCount, sortPhoneNumberFile) = ParseCommandLine(Arguments);
            // Create input file of phone numbers.
            var inputFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "InputPhoneNumbers.txt");
            var outputFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "OutputPhoneNumbers.txt");
            ThreadsafeConsole.WriteLine("Creating input file of phone numbers.", ConsoleColor.White, stopwatch);
            await CreateInputFile(inputFilename, phoneNumberCount);
            ThreadsafeConsole.WriteLine("Done.", ConsoleColor.White, stopwatch);
            // Create sorted output file of phone numbers.
            ThreadsafeConsole.WriteLine("Creating output file of phone numbers.", ConsoleColor.White, stopwatch);
            var sortStart = stopwatch.Elapsed;
            await sortPhoneNumberFile(inputFilename, outputFilename);
            var sortEnd = stopwatch.Elapsed;
            var sortDuration = sortEnd - sortStart;
            ThreadsafeConsole.WriteLine("Done.", ConsoleColor.White, stopwatch);
            ThreadsafeConsole.WriteLine($"Sort took {sortDuration.TotalSeconds.ToString(_elapsedSecondsFormat)} seconds.");
        }


        private static (int PhoneNummberCount, Func<string, string, Task> SortPhoneNumberFile) ParseCommandLine(IReadOnlyList<string> Arguments)
        {
            if ((Arguments.Count < 1) || !int.TryParse(Arguments[0], out var phoneNumberCount)) throw new ArgumentException("Specify a count of phone numbers.");
            var sortMethod = (Arguments.Count > 1) ? Arguments[1].ToLower() : null;
            Func<string, string, Task> sortPhoneNumberFile = sortMethod switch
            {
                "naive" => SortPhoneNumberFileNaive,
                "bitwise" => SortPhoneNumberFileBitwise,
                _ => throw new ArgumentException(sortMethod is null
                    ? "Specify a sort method name."
                    : $"{sortMethod} sort method not supported.")
            };
            return (phoneNumberCount, sortPhoneNumberFile);
        }


        private static async Task CreateInputFile(string InputFilename, int PhoneNumberCount)
        {
            var random = new Random();
            var phoneNumbers = new HashSet<int>(PhoneNumberCount);
            await using (var streamWriter = File.CreateText(InputFilename))
            {
                while (phoneNumbers.Count < PhoneNumberCount)
                {
                    // Get random positive or zero integer in phone number range.
                    var phoneNumber = random.Next(0, _exclusiveMaxPhoneNumber);
                    if (phoneNumbers.Contains(phoneNumber)) continue; // Don't repeat phone number.
                    var phoneNumberChars = phoneNumber.ToString("000-0000").ToCharArray();
                    // Skip invalid phone number.  See https://en.wikipedia.org/wiki/North_American_Numbering_Plan
                    if ((phoneNumberChars[0] == '0') || (phoneNumberChars[0] == '1')) continue;
                    if (phoneNumberChars[1] == '1' && (phoneNumberChars[2] == '1')) continue;
                    phoneNumbers.Add(phoneNumber);
                    await streamWriter.WriteLineAsync(phoneNumber.ToString(_phoneNumberFormat));
                }
            }
        }


        private static async Task SortPhoneNumberFileNaive(string InputFilename, string OutputFilename)
        {
            var sortedPhoneNumbers = new SortedSet<string>();
            // Read phone numbers from input file and add to sorted set.
            using (var streamReader = File.OpenText(InputFilename))
            {
                while (!streamReader.EndOfStream)
                {
                    var phoneNumber = await streamReader.ReadLineAsync();
                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (!phoneNumber.IsNullOrEmpty()) sortedPhoneNumbers.Add(phoneNumber);
                }
            }
            // Iterate over sorted set, writing phone numbers to output file.
            await using (var streamWriter = File.CreateText(OutputFilename))
            {
                foreach (var phoneNumber in sortedPhoneNumbers) await streamWriter.WriteLineAsync(phoneNumber);
            }
        }


        private static async Task SortPhoneNumberFileBitwise(string InputFilename, string OutputFilename)
        {
            // Create array to track phone numbers via bitwise operations.
            var length = Math.DivRem(_exclusiveMaxPhoneNumber, _bitsPerInt, out var remainder);
            if (remainder > 0) length++;
            var phoneNumberBits = new int[length];
            for (var index = 0; index < length; index++) phoneNumberBits[index] = 0;
            // Read phone numbers from input file.
            using (var streamReader = File.OpenText(InputFilename))
            {
                while (!streamReader.EndOfStream)
                {
                    // Convert phone number to integer.
                    var phoneNumberText = await streamReader.ReadLineAsync();
                    if (phoneNumberText.IsNullOrEmpty()) continue;
                    // ReSharper disable once PossibleNullReferenceException
                    var phoneNumber = int.Parse($"{phoneNumberText.Substring(0, 3)}{phoneNumberText.Substring(4, 4)}"); // Remove dash before converting.
                    // Set bit in phone number array to indicate phone number is included in input file.
                    var (index, mask) = GetPhoneNumberMask(phoneNumber);
                    phoneNumberBits[index] |= mask;
                }
            }
            // Iterate over array, writing phone number to output file if bit is set.
            await using (var streamWriter = File.CreateText(OutputFilename))
            {
                for (var index = 0; index < phoneNumberBits.Length; index++)
                {
                    for (var maskIndex = 0; maskIndex < _bitsPerInt; maskIndex++)
                    {
                        var mask = GetMask(maskIndex);
                        if ((phoneNumberBits[index] & mask) == 0) continue; // Phone number not included in input file.
                        var phoneNumber = (index * _bitsPerInt) + maskIndex;
                        await streamWriter.WriteLineAsync(phoneNumber.ToString(_phoneNumberFormat));
                    }
                }
            }
        }


        private static (int Index, int Mask) GetPhoneNumberMask(int PhoneNumber)
        {
            var index = Math.DivRem(PhoneNumber, _bitsPerInt, out var maskIndex);
            var mask = GetMask(maskIndex);
            return (index, mask);
        }


        private static int GetMask(int MaskIndex) => 1 << MaskIndex;
    }
}