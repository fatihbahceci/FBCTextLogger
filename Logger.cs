using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace FBCTextLogger
{

    /// <summary>
    /// 
    /// </summary>
    public class Logger : IDisposable
    {
        private enum ELogAction
        {
            InitChannel, DisposeChannel, WriteLog
        }

        private class LogAction
        {
            public ELogAction Action { get; set; }
            public string ChannelName { get; set; }
            public string Log { get; set; }
            public object[] prms { get; set; }

            public LoggerOptions Options { get; set; }
        }

        private static ConcurrentDictionary<string, Logger> channels = new ConcurrentDictionary<string, Logger>();
        private static ConcurrentQueue<LogAction> queue = new ConcurrentQueue<LogAction>();
        private volatile static bool isConsuming = false;
        /// <summary>
        /// Write log to main channel
        /// </summary>
        /// <param name="s"></param>
        /// <param name="ss"></param>
        public static void Log(string s, params object[] ss)
        {
            LogC("", s, ss);
        }
        /// <summary>
        /// Write log to channel by named as.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="s"></param>
        /// <param name="ss"></param>
        public static void LogC(string channelName, string s, params object[] ss)
        {
            queue.Enqueue(new LogAction()
            {
                Action = ELogAction.WriteLog,
                ChannelName = channelName,
                Log = s,
                prms = ss
            });
            consumeWorks();
        }

        private static void consumeWorks()
        {
            if (!isConsuming)
            {
                new Thread(x => { doConsumeWorks(); }).Start();
            }
        }

        private static void InitChannelInternal(LoggerOptions opt)
        {
            if (channels.ContainsKey(opt.ChannelName))
            {
                //if is main channel. Don't touch! (Main Channel name = "")
                if (!string.IsNullOrEmpty(opt.ChannelName))
                {
                    try
                    {
                        DisposeChannel(opt.ChannelName);
                    }
                    catch { }
                    channels[opt.ChannelName] = new Logger(opt);

                }
            }
            else
            {
                channels[opt.ChannelName ?? ""] = new Logger(opt);
            }
        }

        private static void DisposeChannelInternal(string channelName)
        {
            //if is main channel dont dispose it.  (Main Channel name = "")
            if (!string.IsNullOrEmpty(channelName) && channels.ContainsKey(channelName))
            {
                var channel = channels[channelName];
                channel.Dispose();
                channels.TryRemove(channelName, out channel);
            }
        }
        /// <summary>
        /// Wait until all task(s) consumed
        /// </summary>
        /// <param name="timeout">Milliseconds</param>
        public static void WaitConsumer(int timeout = -1)
        {
            if (timeout > 0)
            {
                var s = DateTime.Now;
                while (isConsuming && (DateTime.Now - s).TotalMilliseconds < timeout)
                {
                    Thread.Sleep(333);
                }

            }
            else
            {
                while (isConsuming)
                {
                    Thread.Sleep(333);
                }
            }
        }


        private static void doConsumeWorks()
        {
            if (!isConsuming)
            {
                try
                {
                    isConsuming = true;
                    LogAction act = null;
                    while (queue.Count > 0)
                    {

                        if (queue.TryDequeue(out act))
                        {
                            try
                            {
                                switch (act.Action)
                                {
                                    case ELogAction.InitChannel:
                                        LoggerOptions opt = act.Options ?? new LoggerOptions(!string.IsNullOrEmpty(act.ChannelName) ? act.ChannelName : "");
                                        InitChannelInternal(opt);
                                        break;
                                    case ELogAction.DisposeChannel:
                                        DisposeChannelInternal(act.ChannelName);
                                        break;
                                    case ELogAction.WriteLog:
                                        string c = act.ChannelName ?? "";
                                        if (!channels.ContainsKey(c))
                                        {
                                            //if this isn't main channel
                                            if (!string.IsNullOrEmpty(c))
                                            {
                                                Logger.Log("Automatically creating new channel named: " + c + ".");

                                            } //else don't log anything
                                            channels[c] = new Logger(new LoggerOptions(c, true, "{0} -> "));
                                        }
                                        channels[c].WriteLog(act.Log, act.prms);
                                        break;
                                }
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                finally
                {
                    isConsuming = false;
                }
            }
        }
        public static void InitChannel(LoggerOptions opt)
        {
            queue.Enqueue(new LogAction()
            {
                Action = ELogAction.InitChannel,
                ChannelName = opt != null ? opt.ChannelName : "",
                Log = null,
                prms = null,
                Options = opt
            });
            consumeWorks();
        }

        public static void DisposeChannel(string channelName)
        {
            queue.Enqueue(new LogAction()
            {
                Action = ELogAction.DisposeChannel,
                ChannelName = channelName,
                Log = null,
                prms = null
            });
            consumeWorks();
        }

        public Logger(LoggerOptions options)
        {
            this.opt = options;
        }

        private TextWriter writer;
        private object writerLock = new object();
        //private static ConcurrentQueue<string> lines;
        //private static volatile bool isConsuming = false;
        //private static void consumeLines()
        //{

        //}
        static readonly string INVALID_FILE_FOLDER_CHARS = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        private string CheckInvalidFileNameChars(string illegal)
        {
            if (string.IsNullOrEmpty(illegal))
            {
                return illegal;
            }
            //string illegal = "\"M\"\\a/ry/ h**ad:>> a\\/:*?\"| li*tt|le|| la\"mb.?";
            foreach (char c in INVALID_FILE_FOLDER_CHARS)
            {
                illegal = illegal.Replace(c.ToString(), "_");
            }
            return illegal;
        }

        private void createNewWriter()
        {
            totalBytesWritten = 0;
            if (writer != null)
            {
                try { writer.Flush(); } catch { }
                try { writer.Close(); } catch { }
                writer = null;
            }
            var prc = Process.GetCurrentProcess();
            Log("App Name: {0}, Process Name: {1}", Path.GetFileName(prc.MainModule.FileName), prc.ProcessName);
            string fullPath = prc.MainModule.FileName;
            ////18:30:37 - 838->App Name: post - commit.exe, Process Name: post - commit
            ////18:30:37 - 839->Full path::C:\NanodemsV2\NDIS\trunk\ExternalIntegrations\SVN_TEAMS_INTEGRATION\PostHookCommit\bin\post - commit.exe

            //string fullPath = Assembly.GetExecutingAssembly().Location;
            string workingPath = (opt.LogDirectoryPath ?? "").Trim();
            if (string.IsNullOrEmpty(workingPath))
            {
                workingPath = Path.GetDirectoryName(fullPath);
            }
            string logFilePath = opt.LogFileNamePrefix;
            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = Path.GetFileNameWithoutExtension(fullPath);
            }
            if (!Directory.Exists(workingPath))
            {
                Directory.CreateDirectory(workingPath);
            }
            string suffix = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string channelNameFile = CheckInvalidFileNameChars(opt.ChannelName ?? "");
            logFilePath = Path.Combine(workingPath, $"{logFilePath}_{(string.IsNullOrEmpty(channelNameFile) ? "" : channelNameFile + "_")}{suffix}.log");
            writer = new StreamWriter(logFilePath, true);
        }

        private volatile int totalBytesWritten = 0;
        private LoggerOptions opt;

        private bool isNewLogFileRequired()
        {
            //100 MB
            return writer == null || totalBytesWritten > (1024 * 1024 * 100);
        }

        public static string ConvertStringFor(string s, LoggerOptions opt, params object[] prms)
        {
            if (prms != null && prms.Length > 0)
            {
                try { s = String.Format(s, prms); } catch { }
            }
            if (opt != null)
            {
                if (opt.AddDateTimeStampEachLog)
                {
                    if (string.IsNullOrEmpty(opt.DateTimeStampFormat))
                    {
                        s = DateTime.Now + s;
                    }
                    else
                    {
                        try
                        {
                            s = string.Format(opt.DateTimeStampFormat, DateTime.Now) + s;
                        }
                        catch
                        {
                            s = DateTime.Now + s;
                        }
                    }
                }
            }
            return s;
        }
        public void WriteLog(string s, params object[] prms)
        {

            lock (writerLock)
            {
                if (isNewLogFileRequired())
                {
                    createNewWriter();
                }
                s = ConvertStringFor(s, opt, prms);
                writer.WriteLine(s);
                totalBytesWritten += s != null ? s.Length : 0;
                writer.Flush();
            }
        }

        public void Dispose()
        {
            if (writer != null)
            {
                try { writer.Flush(); } catch { }
                try { writer.Close(); } catch { }
                writer = null;
            }
        }
    }
}
/*
         public static void Test()
        {

            Logger.InitChannel(new LoggerOptions("MyPrivateChannel", true, "<<<<<{0}>>>>>:"));
            Logger.InitChannel(new LoggerOptions("MyPrivateChannel2", false, null));
            new Thread(x =>
            {
                for (; ; )
                {
                    Logger.Log("This is a default log");
                    Thread.Sleep(50);
                }
            }).Start();
            new Thread(x =>
            {
                for (; ; )
                {
                    Logger.LogC("AutoCreated", "This is  a log with not manuel initialized channel");
                    Thread.Sleep(150);
                }
            }).Start();
            new Thread(x =>
            {
                for (; ; )
                {
                    Logger.LogC("MyPrivateChannel", "This is  a log with manuel initialized channel named: MyPrivateChannel");
                    Thread.Sleep(250);
                }
            }).Start();
            new Thread(x =>
            {
                for (; ; )
                {
                    Logger.LogC("MyPrivateChannel2", "This is  a log with manuel initialized channel named: MyPrivateChannel2");
                    Thread.Sleep(350);
                }
            }).Start();

            new Thread(x =>
            {
                for (; ; )
                {
                    Logger.LogC("AngryChannel", "This is  a log with manuel initialized channel named: AngryChannel");
                    Thread.Sleep(1);
                }
            }).Start();
        }
     */
