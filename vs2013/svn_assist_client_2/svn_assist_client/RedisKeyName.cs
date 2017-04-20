using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svn_assist_client
{
    public static class RedisKeyName
    {
        public const string binTagPrefix = "svn_assist:config:server:bin_tag:";
        public const string tagIDSet = "svn_assist:config:server:bin_tag:id:set";
        public const string serverIDSet = "svn_assist:config:server:id:set";
        public const string serverPrefix = "svn_assist:config:server:";
        public const string usernameSet = "svn_assist:users:id:set";
        public const string userInfoPrefix = "svn_assist:users:";
        public const string userAuthorityPrefix = "svn_assist:users:authority:";
        public const string currentTrunkRevisionKey = "svn_assist:config:version:compile_trunk:number";
        public const string currentBranchRevisionKey = "svn_assist:config:version:compile_branch:number";
        public const string svnBaseDirKey = "svn_assist:config:svn_base:dir";
        public const string workDirKey = "svn_assist:config:ss_worker:work_dir";
        public const string commandSet = "svn_assist:config:command:set";
        public const string commandPrefix = "svn_assist:config:command:";
        public const string svnLogIDSet = "svn_assist:config:svn_log:id:set";
        public const string svnLogPrefix = "svn_assist:config:svn_log:";
        public const string taskPrefix = "svn_assist:task:";
        public const string waitingTaskList = "svn_assist:task_list:wait";
        public const string waitingTaskListCopy = "svn_assist:task_list:message";
        public const string finishedTaskList = "svn_assist:task_list:done";
        public const string currentBranchOfServerPrefix = "svn_assist:server:switch:";
        public const string subscribeKey = "svn_assist:task:done";
        public const string trunkCodeCommandSet = "svn_assist:config:command:trunkcode:set";
        public const string branchCodeCommandSet = "svn_assist:config:command:branchcode:set";
        public const string serverCommandSet = "svn_assist:config:command:server:set";
        public const string trunkCodeCommandPrefix = "svn_assist:config:command:";
        public const string branchCodeCommandPrefix = "svn_assist:config:command:";
        public const string serverCommandPrefix = "svn_assist:config:command:";
        public const string trunkCodeRevesionShellKey = "svn_assist:config:version:compile_trunk:script";
        public const string branchCodeRevesionShellKey = "svn_assist:config:version:compile_branch:script";
    }
}