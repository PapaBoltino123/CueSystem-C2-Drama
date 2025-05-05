using System;
using System.Data;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using NAudio.CoreAudioApi;
using NAudio.Wave;

class Program
{
    static List<Speaker> ConnectedSpeakers = new List<Speaker>();
    static List<Microphone> ConnectedMics = new List<Microphone>();
    static string Mode = "Devised";
    static bool StopDetectingInput = false;

    static void Main()
    {
        string Choice = string.Empty;
        Console.WriteLine("\nMode set to devised initially.");

        do
        {
            DisplayOptionsMenu();
            Choice = Console.ReadLine();

            switch (Choice.ToLower())
            {
                case "a":
                    RunCueProgram();
                    Choice = string.Empty;
                    break;
                case "b":
                    ConnectDevice("Speaker");
                    Choice = string.Empty;
                    break;
                case "c":
                    ConnectDevice("Microphone");
                    Choice = string.Empty;
                    break;
                case "d":
                    Mode = "Text";
                    Choice = string.Empty;
                    break;
                case "e":
                    Mode = "Devised";
                    Choice = string.Empty;
                    break;
                case "q":
                    Console.WriteLine("Exiting...");
                    break;
                default:
                    Console.WriteLine("Unable to determine choice from input.");
                    Choice = string.Empty;
                    break;


            }
        } while (Choice == string.Empty);
    }

    static void DisplayOptionsMenu()
    {
        Console.WriteLine("\nPlease select an option from below:");
        Console.WriteLine("a) Run cue program");
        Console.WriteLine("b) Connect a speaker");
        Console.WriteLine("c) Connect a microphone");
        Console.WriteLine("d) Switch to text");
        Console.WriteLine("e) Switch to devised");
        Console.Write("Enter letter associated: ");
    }
    static void ConnectDevice(string DeviceType)
    {
        List<MMDevice> AudioDevices = GetAvailableAudioDevices(DeviceType == "Speaker" ? DataFlow.Render : DataFlow.Capture);
        bool AudioDevicesEmpty = AudioDevices.Count == 0;
        string Choice = string.Empty;

        while (!AudioDevicesEmpty && Choice == string.Empty)
        {
            char LetterChoice = 'a';
            string MenuOptions = string.Empty;
            Console.WriteLine("\nPlease select an option below:");

            foreach (MMDevice Device in AudioDevices)
            {
                MenuOptions += $"{LetterChoice}) {Device.FriendlyName}\n";
                LetterChoice++;
            }
            Console.Write($"{MenuOptions}Enter letter associated: ");
            Choice = Console.ReadLine().ToLower();

            string[] AvailableLetters = Enumerable.Range('a', (int)LetterChoice + 1 - 'a').
                Select(n => ((char)n).ToString())
                .ToArray();

            if (!AvailableLetters.Contains(Choice))
            {
                Console.WriteLine("Unable to determine choice from input.");
                Choice = string.Empty;
            }
            else
            {
                string Selected = MenuOptions.Split('\n')
                    .Where(Option => Option[0] == Choice[0])
                    .First();
                Console.WriteLine(Selected);
                Console.Write($"Enter {DeviceType.ToLower()} name for ease of access later: ");
                string MainName = Console.ReadLine();
                string FriendlyName = Selected.Substring(3);
                MMDevice Device = AudioDevices.Where(AudioDevice => AudioDevice.FriendlyName == FriendlyName)
                    .First();

                if (DeviceType == "Speaker") ConnectedSpeakers.Add(new Speaker(MainName, FriendlyName, Device));
                else ConnectedMics.Add(new Microphone(MainName, FriendlyName, Device));
            }
        }
    }
    static List<MMDevice> GetAvailableAudioDevices(DataFlow Flow)
    {
        List<MMDevice> AudioDevices = new List<MMDevice>();
        AudioDevices = new MMDeviceEnumerator()
            .EnumerateAudioEndPoints(Flow, DeviceState.Active)
            .Where(Device =>
                !((Flow == DataFlow.Render ?
                ConnectedSpeakers.Select(Speaker => Speaker.ToAudioDevice()) :
                ConnectedMics.Select(Mic => Mic.ToAudioDevice()))
                .Select(AudioDevice => AudioDevice.FriendlyName)
                .Contains(Device.FriendlyName)))
            .ToList();
        AudioDevices = AudioDevices.Where(Speaker => !Speaker.FriendlyName.Contains("Headset")).ToList();
        return AudioDevices;
    }
    static void RunCueProgram()
    {
        StopDetectingInput = false;

        Task.Run(() => CheckForInput());
        while (!StopDetectingInput) Thread.Sleep(100);
    }
    static Dictionary<int, string> GetCommandPairs(string Mode)
    {
        Dictionary<int, string> CommandPairs = new Dictionary<int, string>();

        if (Mode == "Devised")
        {
            CommandPairs = new Dictionary<int, string>()
            {
                { 1, "set volume L R 100\nplay L R SFX1"}, //scene start
                { 3, "set volume M 100\nset volume L R 0\nplay M L R SFX2"}, //scene start
                { 4, "set volume M 75"}, //distortion
                { 5, "fade M 0 3\nfade L R 75 6"}, //butoh
                { 6, "fade L R 100 1"}, //violin
                { 7, "fade L R 0 5\nfade M 100 5"}, //end
                { 8, "set volume M 66\nset volume L R 0\nplay M L R SFX3"}, //vomit
                { 9, "fade M 25 3\nfade L R 100 3"}, //panned vomit forward
                { 10, "set volume L R 90\nplay L SFX4\nplay R SFX5"}, //play echo
                { 11, "fade L R 100 5"}, //increase echo
                { 12, "set volume M L R 75\nplay L M R SFX6"}, //play phone call
                { 13, "fade M L R 100 6"}, //increase phone call
                { 2, "set volume L R 55" }
            };
        }
        else if (Mode == "Text")
        {
            CommandPairs = new Dictionary<int, string>()
            {
                { 1, "set volume L 100\nset volume M 25\nset volume R 30\nplay M Rain\nplay M Wind"}, //play fireplace, rain and wind
                { 6, "play R Open"}, //open door
                { 7, "set volume R 30\nplay R Tree"}, //play tree blowing sound
                { 8, "play R Shut"}, //shut door and decrease rain volume back to normal
                { 9, "set volume L 20\nset volume R 30\nplay L R flapping\nfade L 100 5\nfade R 5 5" }, //play and pan random crow sound
                { 2, "set volume M 80\nplay M Thunder"}, //play thunder 
                { 3, "fade M 25 1"}, //reduce volume from thunder back to normal
                { 10, "play M crowcall" },
                { 4, "set volume B 70\nplay B Fireplace" },
                { 5, "play R Footsteps" }
            };
        }

        return CommandPairs;
    }

    static string GetRandomCrowCall()
    {
        string path = "Sounds/Text";
        string[] calls = Directory.GetFiles(path).Where(file => file.Contains("Call")).ToArray();
        Random rng = new Random();
        int randomCall = rng.Next(calls.Length);
        return calls[randomCall].Replace(@"\", @"/");
    }

    static int GetDuration(string path)
    {
        using (var reader = new Mp3FileReader(path))
        {
            TimeSpan duration = reader.TotalTime;
            return (int)Math.Floor(duration.TotalSeconds);
        }
    }

    static void CheckForInput()
    {
        Dictionary<int, string> CommandPairs = GetCommandPairs(Mode);
        while (true)
        {
            string input = Console.ReadLine();
            if (input.ToLower() == "q")
            {
                StopDetectingInput = true;
                break;
            }
            if (Char.IsDigit(input[0])
                && (CommandPairs.Keys.Contains(Convert.ToInt32(input.Trim()))
                && input.Length > 0))
            {
                HandleInput(Convert.ToInt32(input.Trim()), CommandPairs);
            }
            Thread.Sleep(100);
        }
    }

    static void HandleInput(int Key, Dictionary<int, string> Pairs)
    {
        List<string> Commands = new List<string>();
        string MatchPattern = @"(?:\b([A-Z])\b)(?: \b([A-Z])\b)*";
        string Cue = Pairs[Key];

        foreach (string Instruction in Cue.Split('\n'))
        {
            List<Speaker> Speakers = new List<Speaker>();
            List<Microphone> Mics = new List<Microphone>();
            string OtherDetails = string.Empty;
            string TaskType = Instruction.Substring(0, Instruction.ToList()
                .FindIndex(Char.IsUpper) - 1);

            Match DeviceMatch = Regex.Match(Instruction, MatchPattern);
            if (DeviceMatch.Success)
            {
                try
                {
                    foreach (string Speaker in DeviceMatch.Value.Split(' '))
                    {
                        Speakers.Add(ConnectedSpeakers
                            .Where(S => S.DeviceName.ToUpper()[0] == Speaker[0])
                            .First());
                    }
                }
                catch { }

                OtherDetails = Instruction.Substring(DeviceMatch.Index + DeviceMatch.Length + 1);
                Console.WriteLine(OtherDetails);
            }

            foreach (string Speaker in Speakers.Select(s => s.DeviceName))
                Commands.Add($"{TaskType}*{Speaker}*{OtherDetails}");
        }

        ExecuteInstructions(Commands);
    }

    static void ExecuteInstructions(List<string> Commands)
    {
        ManualResetEventSlim StartGate = new ManualResetEventSlim(false);
        List<Task> Tasks = new List<Task>();
        string FilePath = @"Sounds/" + ((Mode == "Devised") ? @"Devised/" : @"Text/");
        string crowFile = string.Empty;

        foreach (string Command in Commands)
        {
            Tasks.Add(Task.Run(() =>
            {
                StartGate.Wait();
                Speaker speaker;
                Microphone microphone;

                switch (Command.Split('*')[0])
                {
                    case "play":
                        speaker = ConnectedSpeakers.Where(s => Command.Split('*')[1] == s.DeviceName).First();
                        string PlayType = Command.Split('*')[2];
                        if (PlayType.Contains("recording"))
                        {
                            Thread PlaybackThread = new Thread(() =>
                            {
                                Microphone mic = ConnectedMics.Where(M => M.DeviceName.ToUpper()[0] == PlayType.Split(' ')[1][0]).First();
                                speaker.Play(mic.Format);
                                mic.RecordAudio((buffer, length) =>
                                {
                                    speaker.Feed(buffer, length);
                                });
                            });
                            PlaybackThread.Start();
                        }
                        else
                        {
                            if (PlayType == "crowcall")
                            {
                                if (crowFile == string.Empty) crowFile = GetRandomCrowCall();
                                PlayType = crowFile.Split('/').Last().Substring(0, crowFile.Split('/').Last().Length - 4);
                            }
                            string path = FilePath + PlayType + ".mp3";
                            speaker.Play(path);
                        }
                        break;
                    case "stop":
                        speaker = ConnectedSpeakers.Where(s => Command.Split('*')[1] == s.DeviceName).First();
                        string StopType = Command.Split('*')[2];
                        speaker.Stop();
                        if (StopType.Contains("recording"))
                        {
                            Thread PlaybackThread = new Thread(() =>
                            {
                                Microphone mic = ConnectedMics.Where(M => M.DeviceName.ToUpper()[0] == StopType.Split(' ')[1][0]).First();
                                mic.StopRecording();
                            });
                            PlaybackThread.Start();
                        }
                        break;
                    case "set volume":
                        speaker = ConnectedSpeakers.Where(s => Command.Split('*')[1] == s.DeviceName).First();
                        string Number = Command.Split('*')[2];
                        double Volume = Number.Length == 3 ? Convert.ToDouble($"{Number[0]}") :
                        Number.Length == 2 ? Convert.ToDouble($"0.{Number}") : Convert.ToDouble($"0.0{Number}");
                        speaker.SetVolume(Volume);
                        break;
                    case "fade":
                        speaker = ConnectedSpeakers.Where(s => Command.Split('*')[1] == s.DeviceName).First();
                        string Numbers = Command.Split('*')[2];
                        int duration = Convert.ToInt32(Numbers.Split(' ')[1]);
                        if (duration == 1050)
                        {
                            Task.Run(() =>
                            {
                                while (crowFile == string.Empty)
                                {
                                    Thread.Sleep(10);
                                }
                                duration = GetDuration(crowFile);
                                Console.WriteLine("Duration "+ duration);
                                string VolumeAsString = Numbers.Split(" ")[0];
                                double Vol = VolumeAsString.Length == 3 ? Convert.ToDouble($"{VolumeAsString[0]}") :
                                VolumeAsString.Length == 2 ? Convert.ToDouble($"0.{VolumeAsString}") : Convert.ToDouble($"0.0{VolumeAsString}");
                                speaker.ChangeVolumeGradually(duration, Vol);
                            });
                        }
                        string VolumeAsString = Numbers.Split(" ")[0];
                        double Vol = VolumeAsString.Length == 3 ? Convert.ToDouble($"{VolumeAsString[0]}") :
                        VolumeAsString.Length == 2 ? Convert.ToDouble($"0.{VolumeAsString}") : Convert.ToDouble($"0.0{VolumeAsString}");
                        speaker.ChangeVolumeGradually(duration, Vol);
                        break;
                }
            }));
        }
        crowFile = string.Empty;
        StartGate.Set();
    }
}

public class Speaker : AudioDevice
{
    public WasapiOut Output;
    private BufferedWaveProvider Provider;
    public Speaker(string DeviceName, string FriendlyName, MMDevice Device) : base(DeviceName, FriendlyName, Device)
    {
        this.DeviceName = DeviceName;
        this.FriendlyName = FriendlyName;
        this.Device = Device;
    }
    public void Play(string Filepath)
    {
        Task.Run(() =>
        {
            using (var reader = new AudioFileReader(Filepath))
            {
                var localOutput = new WasapiOut(Device, AudioClientShareMode.Shared, true, 100);

                localOutput.PlaybackStopped += (s, e) =>
                {
                    localOutput.Dispose();

                    // Only null Output if it's still pointing to this instance
                    if (ReferenceEquals(Output, localOutput))
                    {
                        Output = null;
                    }

                    if (e.Exception != null)
                        Console.WriteLine($"Playback error: {e.Exception.Message}");
                    else
                        Console.WriteLine("Playback finished.");
                };

                Output = localOutput; // only assign this after the event is safely hooked
                localOutput.Init(reader);
                localOutput.Play();
            }
        });
    }
    public void Play(WaveFormat Format)
    {
        Provider = new BufferedWaveProvider(Format)
        {
            DiscardOnBufferOverflow = true
        };

        var localOutput = new WasapiOut(Device, AudioClientShareMode.Shared, false, 100);

        localOutput.PlaybackStopped += (s, e) =>
        {
            localOutput.Dispose();

            // Only set Output to null if it's still pointing to this instance
            if (ReferenceEquals(Output, localOutput))
            {
                Output = null;
            }

            if (e.Exception != null)
                Console.WriteLine($"Playback error: {e.Exception.Message}");
            else
                Console.WriteLine("Playback finished.");
        };

        Output = localOutput;
        localOutput.Init(Provider);
        localOutput.Play();
    }

    public void Feed(byte[] buffer, int length)
    {
        Provider.AddSamples(buffer, 0, length);
    }

    public void ChangeVolumeGradually(int durationInSeconds, double endVolume)
    {
        Task.Run(() =>
        {
            AudioEndpointVolume deviceVolume = Device.AudioEndpointVolume;
            float currentVolume = deviceVolume.MasterVolumeLevelScalar;
            float targetVolume = (float)Math.Clamp(endVolume, 0.0, 1.0);

            if (currentVolume == targetVolume)
                return;

            int steps = durationInSeconds * 10; // 10 steps per second
            float stepSize = (targetVolume - currentVolume) / steps;

            for (int i = 0; i < steps; i++)
            {
                currentVolume += stepSize;
                deviceVolume.MasterVolumeLevelScalar = Math.Clamp(currentVolume, 0.0f, 1.0f);
                Thread.Sleep(100); // 100ms per step
            }

            // Ensure final volume is exactly target
            deviceVolume.MasterVolumeLevelScalar = targetVolume;
     
        });
    }
    public void SetVolume(double volume)
    {
        Task.Run(() =>
        {
            Console.WriteLine(volume);
            AudioEndpointVolume deviceVolume = Device.AudioEndpointVolume; ;
            deviceVolume.MasterVolumeLevelScalar = (float)volume;
        });
    }
    public void Stop()
    {
        Output.Stop();
        Output.Dispose();
        Output = null;
        Provider = null;
    }
}

public class Microphone : AudioDevice
{
    public WasapiCapture CapturedAudio;
    private MemoryStream Stream;
    public WaveFormat Format;

    public Microphone(string DeviceName, string FriendlyName, MMDevice Device) : base(DeviceName, FriendlyName, Device)
    {
        this.DeviceName = DeviceName;
        this.FriendlyName = FriendlyName;
        this.Device = Device;
        Format = new WasapiCapture(Device).WaveFormat;
    }
    public void RecordAudio(Action<byte[], int> OnBuffer)
    {
        CapturedAudio = new WasapiCapture(Device);
        CapturedAudio.DataAvailable += (s, e) =>
        {
            OnBuffer?.Invoke(e.Buffer, e.BytesRecorded);
        };

        CapturedAudio.StartRecording();
    }
    public void StopRecording()
    {
        if (CapturedAudio != null)
        {
            CapturedAudio.StopRecording();
            CapturedAudio.Dispose();
            CapturedAudio = null;
        }
    }
}

public class AudioDevice
{
    public string FriendlyName { get; set; }
    public string DeviceName { get; set; }
    public MMDevice Device { get; set; }

    public AudioDevice(string DeviceName, string FriendlyName, MMDevice Device)
    {
        this.DeviceName = DeviceName;
        this.FriendlyName = FriendlyName;
        this.Device = Device;
    }

    public AudioDevice ToAudioDevice()
    {
        return new AudioDevice(DeviceName, FriendlyName, Device);
    }
}