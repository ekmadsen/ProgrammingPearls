using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ErikTheCoder.Logging;
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            (int phoneNumberCount, Func<string, string, Task> sortPhoneNumberFile) = ParseCommandLine(Arguments);
            // Create input file of phone numbers.
            string inputFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "InputPhoneNumbers.txt");
            string outputFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "OutputPhoneNumbers.txt");
            ThreadsafeConsole.WriteLine("Creating input file of phone numbers.", ConsoleColor.White, stopwatch);
            await CreateInputFile(inputFilename, phoneNumberCount);
            ThreadsafeConsole.WriteLine("Done.", ConsoleColor.White, stopwatch);
            // Create sorted output file of phone numbers.
            ThreadsafeConsole.WriteLine("Creating output file of phone numbers.", ConsoleColor.White, stopwatch);
            TimeSpan sortStart = stopwatch.Elapsed;
            await sortPhoneNumberFile(inputFilename, outputFilename);
            TimeSpan sortEnd = stopwatch.Elapsed;
            TimeSpan sortDuration = sortEnd - sortStart;
            ThreadsafeConsole.WriteLine("Done.", ConsoleColor.White, stopwatch);
            ThreadsafeConsole.WriteLine($"Sort took {sortDuration.TotalSeconds.ToString(_elapsedSecondsFormat)} seconds.");
        }


        private static (int PhoneNummberCount, Func<string, string, Task> SortPhoneNumberFile) ParseCommandLine(IReadOnlyList<string> Arguments)
        {
            if ((Arguments.Count < 1) || !int.TryParse(Arguments[0], out int phoneNumberCount)) throw new ArgumentException("Specify a count of phone numbers.");
            Func<string, string, Task> sortPhoneNumberFile;
            string sortMethod = (Arguments.Count > 1) ? Arguments[1].ToLower() : null;
            switch (sortMethod)
            {
                case "naive":
                    sortPhoneNumberFile = SortPhoneNumberFileNaive;
                    break;
                case "bitwise":
                    sortPhoneNumberFile = SortPhoneNumberFileBitwise;
                    break;
                default:
                    throw new ArgumentException(sortMethod is null ? "Specify a sort method name." : $"{sortMethod} sort method not supported.");
            }
            return (phoneNumberCount, sortPhoneNumberFile);
        }


        private static async Task CreateInputFile(string InputFilename, int PhoneNumberCount)
        {
            Random random = new Random();
            HashSet<int> phoneNumbers = new HashSet<int>(PhoneNumberCount);
            using (StreamWriter streamWriter = File.CreateText(InputFilename))
            {
                while (phoneNumbers.Count < PhoneNumberCount)
                {
                    // Get random positive or zero integer in phone number range.
                    int phoneNumber = random.Next(0, _exclusiveMaxPhoneNumber);
                    if (phoneNumbers.Contains(phoneNumber)) continue; // Don't repeat phone number.
                    char[] phoneNumberChars = phoneNumber.ToString("000-0000").ToCharArray();
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
            SortedSet<string> sortedPhoneNumbers = new SortedSet<string>();
            // Read phone numbers from input file and add to sorted set.
            using (StreamReader streamReader = File.OpenText(InputFilename))
            {
                while (!streamReader.EndOfStream)
                {
                    string phoneNumber = await streamReader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(phoneNumber)) sortedPhoneNumbers.Add(phoneNumber);
                }
            }
            // Iterate over sorted set, writing phone numbers to output file.
            using (StreamWriter streamWriter = File.CreateText(OutputFilename)) { foreach (string phoneNumber in sortedPhoneNumbers) await streamWriter.WriteLineAsync(phoneNumber); }
        }


        private static async Task SortPhoneNumberFileBitwise(string InputFilename, string OutputFilename)
        {
            // Create array to track phone numbers via bitwise operations.
            int length = Math.DivRem(_exclusiveMaxPhoneNumber, _bitsPerInt, out int remainder);
            if (remainder > 0) length++;
            int[] phoneNumberBits = new int[length];
            for (int index = 0; index < length; index++) phoneNumberBits[index] = 0;
            // Read phone numbers from input file.
            using (StreamReader streamReader = File.OpenText(InputFilename))
            {
                while (!streamReader.EndOfStream)
                {
                    // Convert phone number to integer.
                    string phoneNumberText = await streamReader.ReadLineAsync();
                    if (string.IsNullOrEmpty(phoneNumberText)) continue;
                    int phoneNumber = int.Parse($"{phoneNumberText.Substring(0, 3)}{phoneNumberText.Substring(4, 4)}"); // Remove dash before converting.
                    // Set bit in phone number array to indicate phone number is included in input file.
                    (int index, int mask) = GetPhoneNumberMask(phoneNumber);
                    phoneNumberBits[index] |= mask;
                }
            }
            // Iterate over array, writing phone number to output file if bit is set.
            using (StreamWriter streamWriter = File.CreateText(OutputFilename))
            {
                for (int index = 0; index < phoneNumberBits.Length; index++)
                {
                    for (int maskIndex = 0; maskIndex < _bitsPerInt; maskIndex++)
                    {
                        int mask = GetMask(maskIndex);
                        if ((phoneNumberBits[index] & mask) == 0) continue; // Phone number not included in input file.
                        int phoneNumber = (index * _bitsPerInt) + maskIndex;
                        streamWriter.WriteLine(phoneNumber.ToString(_phoneNumberFormat));
                    }
                }
            }
        }


        private static (int Index, int Mask) GetPhoneNumberMask(int PhoneNumber)
        {
            int index = Math.DivRem(PhoneNumber, _bitsPerInt, out int maskIndex);
            int mask = GetMask(maskIndex);
            return (index, mask);
        }


        private static int GetMask(int MaskIndex) => 1 << MaskIndex;
    }
}