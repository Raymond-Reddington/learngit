using SharpSvn;
using System;
using System.Collections.ObjectModel;

namespace svn_assist_client
{
    class Log
    {
        long revision;
        string author;
        DateTime time;
        string message;
        string short_message;
        Collection<SvnChangeItem> paths;
        public Log(SvnLogEventArgs e)
        {
            revision = e.Revision;
            author = e.Author;
            time = e.Time.AddHours(8);
            message = e.LogMessage;
            paths = e.ChangedPaths;
            if(message != null && message.Contains("\n"))
            {
                short_message = message.Split('\n')[0];
            }
            else
            {
                short_message = message;
            }
        }
        public string Short_Message 
        {
            get { return short_message; }
        }
        public Collection<SvnChangeItem> Paths
        {
            get { return paths; }
        }
        public long Revision
        {
            get { return revision; }
        }
        public string Author
        {
            get { return author; }
        }
        public DateTime Time
        {
            get { return time; }
        }
        public string Message
        {
            get { return message; }
        }
    }
    class ChangedPath
    {
        SvnChangeAction action;
        string path;
        public ChangedPath(SvnChangeItem e)
        {
            action = e.Action;
            path = e.Path;
        }
        public SvnChangeAction Action 
        {
            get { return action; }
        }
        public string Path
        {
            get { return path; }
        }
    }
}
