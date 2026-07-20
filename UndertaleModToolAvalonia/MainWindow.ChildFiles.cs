using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    // child-data-file support: opening an embedded asset that lives in an external file (e.g. an audiogroup .dat)
    // launches a second instance of the tool on that file and streams open-requests to it over a named pipe.
    // 1:1 port of the wpf OpenChildFile / CloseChildFiles / ListenChildConnection.
    public partial class MainWindow
    {
        public Dictionary<string, NamedPipeServerStream> childFiles = new();

        /// <summary>opens (or reuses) a child instance of the tool viewing <paramref name="filename"/>, and asks it
        /// to select the given chunk item.</summary>
        public void OpenChildFile(string filename, string chunkName, int itemIndex)
        {
            if (childFiles.ContainsKey(filename))
            {
                try
                {
                    var existingWriter = new StreamWriter(childFiles[filename]);
                    existingWriter.WriteLine(chunkName + ":" + itemIndex);
                    existingWriter.Flush();
                    return;
                }
                catch (IOException e)
                {
                    Debug.WriteLine(e);
                    childFiles.Remove(filename);
                }
            }

            string key = Guid.NewGuid().ToString();

            string dir = Path.GetDirectoryName(FilePath);
            string childFilePath = Paths.TryJoinVerifyWithinDirectory(dir, filename);
            if (childFilePath is null)
            {
                ScriptError("Failed to open child data file; escaped directory.");
                return;
            }
            // merged binary: the child must be launched into gui mode, so "--gui" leads (see Program.Main)
            Process.Start(new ProcessStartInfo(Environment.ProcessPath, new[] { "--gui", childFilePath, key }));

            var server = new NamedPipeServerStream(key);
            server.WaitForConnection();
            childFiles.Add(filename, server);

            var writer = new StreamWriter(childFiles[filename]);
            writer.WriteLine(chunkName + ":" + itemIndex);
            writer.Flush();
        }

        public void CloseChildFiles()
        {
            foreach (var pair in childFiles)
                pair.Value.Close();
            childFiles.Clear();
        }

        /// <summary>runs in a child instance: connects back to the parent's pipe and opens whatever it's told to.</summary>
        public async Task ListenChildConnection(string key)
        {
            var client = new NamedPipeClientStream(key);
            client.Connect();
            var reader = new StreamReader(client);

            while (true)
            {
                string[] thingToOpen = (await reader.ReadLineAsync()).Split(':');
                if (thingToOpen.Length != 2)
                    throw new Exception("ummmmm");
                if (thingToOpen[0] != "AUDO") // just pretend I'm not hacking it together that poorly
                    throw new Exception("errrrr");
                OpenInTab(Data.EmbeddedAudio[int.Parse(thingToOpen[1])], false, "Embedded Audio");
                Activate();
            }
        }
    }
}
