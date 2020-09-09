namespace FBCTextLogger
{
    public class LoggerOptions
    {
        private string dkfjsdlk_hndjkghnfjkxhgn_jkhg;
        /// <summary>
        /// Unique key
        /// </summary>
        public string ChannelName
        {
            get
            {
                return dkfjsdlk_hndjkghnfjkxhgn_jkhg ?? (dkfjsdlk_hndjkghnfjkxhgn_jkhg = "");
            }
            set
            {
                dkfjsdlk_hndjkghnfjkxhgn_jkhg = value ?? dkfjsdlk_hndjkghnfjkxhgn_jkhg;
            }
        }
        /// <summary>
        /// Default false
        /// </summary>
        public bool AddDateTimeStampEachLog { get; }
        /// <summary>
        /// Default "{0}" or null or Empty string
        /// Example "{0} ->"
        /// </summary>
        public string DateTimeStampFormat { get; }
        /// <summary>
        /// Default: null -> Current assembly path
        /// </summary>
        public string LogDirectoryPath { get; }
        /// <summary>
        /// Default: null -> Current assembly name without extension
        /// </summary>
        public string LogFileNamePrefix { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelName">Unique key</param>
        /// <param name="addDateTimeStampEachLog">Default false</param>
        /// <param name="dateTimeStampFormat">default null</param>
        /// <param name="logDirectoryPath">default null:  Current assembly path</param>
        /// <param name="logFileNamePrefix">default null: Current assembly name without extension</param>
        public LoggerOptions(string channelName, bool addDateTimeStampEachLog = false, string dateTimeStampFormat = null, string logDirectoryPath = null, string logFileNamePrefix = null)
        {
            ChannelName = channelName ?? "";
            AddDateTimeStampEachLog = addDateTimeStampEachLog;
            DateTimeStampFormat = dateTimeStampFormat;
            LogDirectoryPath = logDirectoryPath;
            LogFileNamePrefix = logFileNamePrefix;
        }
    }
}
