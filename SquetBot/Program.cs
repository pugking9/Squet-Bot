using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Audio;
using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace SquetBot
{
    class Program
    {
        static void Main(string[] args) => new Program().Run();

        private static DiscordClient _client;

        static void DownloadYoutubeAudio(string link)
        {
            // Use ProcessStartInfo class
            if(File.Exists("temp.mp3"))
            {
                File.Delete("temp.mp3");
            }
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "youtube-dl.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "-f bestaudio "+link+" --exec \"ffmpeg -i {}  -codec:a libmp3lame -qscale:a 0 temp.mp3 && del {}\"";

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                Console.WriteLine("Falure");
            }
        }

        public static async Task SendAudio(string filePath, IAudioClient _vClient)
        {

            // Simple try and catch.
            try
            {

                var channelCount = _client.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
                var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.

                using (var MP3Reader = new Mp3FileReader(filePath)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
                using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
                {
                    resampler.ResamplerQuality = 30; // Set the quality of the resampler to 60, the highest quality
                    int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                    byte[] buffer = new byte[blockSize];
                    int byteCount;
                    // Add in the "&& playingSong" so that it only plays while true. For our cheesy skip command.
                    // AGAIN
                    // WARNING
                    // YOU NEED
                    // vvvvvvvvvvvvvvv
                    // opus.dll
                    // libsodium.dll
                    // ^^^^^^^^^^^^^^^
                    // If you do not have these, this will not work.
                    while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into our buffer, and keep a loop open while data is present
                    {
                        if (byteCount < blockSize)
                        {
                            // Incomplete Frame
                            for (int i = byteCount; i < blockSize; i++)
                                buffer[i] = 0;
                        }

                        _vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                    }
                    await _vClient.Disconnect();
                }
            }
            catch
            {
                System.Console.WriteLine("Something went wrong. :(");
            }
        }

        public void Run()
        {
            _client = new DiscordClient();
            _client.UsingCommands(x =>
            {
                x.PrefixChar = '$';
                x.HelpMode = HelpMode.Public;
            });
            _client.UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
            });

            _client.Log.Message += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            _client.ExecuteAndWait(async () => {
                await _client.Connect("MzI2NDczNjE1MzU4ODIwMzUz.DCnT6A.kebsUqPxZvdgSy7YT4Zw8l1CMvE", TokenType.Bot);
                await Task.Delay(2000);

                var voiceChannel = _client.Servers.FirstOrDefault().VoiceChannels.ToArray()[1];

                var _vClient = await _client.GetService<AudioService>() // We use GetService to find the AudioService that we installed earlier. In previous versions, this was equivelent to _client.Audio()
                        .Join(voiceChannel); // Join the Voice Channel, and return the IAudioClient.

                await Task.Delay(2000);

                
                //await SendAudio("c:\\users\\pugki\\desktop\\Chick_Willis_-_Nuts_For_Sale_hot_nuts.mp3", _vClient);

                _client.GetService<CommandService>().CreateCommand("greet")
                    .Alias(new string[] { "gr", "hi" })
                    .Description("Greets a person.")
                    .Parameter("GreetedPerson", ParameterType.Required)
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage($"{e.User.Name} greets {e.GetArg("GreetedPerson")}");
                    });

                _client.GetService<CommandService>().CreateCommand("jukebox")
                    .Alias(new string[] { "jukebox" })
                    .Description("Play some dank tunes")
                    .Parameter("link", ParameterType.Required)
                    .Do(e =>
                    {
                        DownloadYoutubeAudio(e.GetArg("link"));
                        SendAudio("temp.mp3", _vClient);
                    });

                _client.GetService<CommandService>().CreateCommand("disconnect")
                    .Alias(new string[] { "dc" })
                    .Description("Forces the bot to disconnect")
                    .Do(async e =>
                    {
                        await e.Channel.SendMessage($"{e.User.Name} called for a disconnect. Bye guys.");
                        await Task.Delay(1000);
                        await _client.Disconnect();
                    });

                _client.GetService<CommandService>().CreateCommand("smajtalk")
                    .Alias(new string[] { "st", "tardspeak" })
                    .Description("Talk just like smaJ")
                    .Parameter("Message",ParameterType.Multiple)
                    .Do( e =>
                    {
                        int percent = 100 - int.Parse(e.Args[1]);
                        int chance;
                        Random rndPerc = new Random();
                        if (percent >= 0)
                        {
                            string message = e.GetArg("Message");
                            var I = 1;
                            Regex regParse = new Regex("[aeiouyAEIOU][^aeiouynxr\\W]");
                            MatchCollection regMatches = regParse.Matches(message);
                            foreach (Match RegM in regMatches)
                            {
                                chance = rndPerc.Next(100);
                                if (chance >= percent)
                                {
                                    message = message.Insert(RegM.Index + I, "n");
                                    Console.WriteLine(RegM.Value);
                                    Console.WriteLine(message);
                                    I++;
                                }
                            }
                            e.Channel.SendTTSMessage($"{e.User.Name} the retard says: {message}");
                        }
                    });
            });
        }

        private void _client_Ready(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
