////////////////////////////////////////////////////////////////////////////
// NavigatorServer.cs - File Server for WPF NavigatorClient Application   //
// ver 2.0                                                                //
// Author:      Jim Fawcett, CST 4-187, Syracuse University               //
//              (315) 443-3948, jfawcett@twcny.rr.com                     //
//              Ankit Singh       , Syracuse University                   //
//              asingh32@syr.edu                                          //
////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines a single NavigatorServer class that returns file
 * and directory information about its rootDirectory subtree.  It uses
 * a message dispatcher that handles processing of all incoming and outgoing
 * messages.
 * 
 * Public Interface:
 * 
 * Maintanence History:
 * --------------------
 * ver 2.0 - 24 Oct 2017
 * - added message dispatcher which works very well - see below
 * - added these comments
 * ver 1.0 - 22 Oct 2017
 * - first release
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;
using CodeAnalysis;
using CsGraph;

namespace Navigator
{
    public class NavigatorServer
    {
        IFileMgr localFileMgr { get; set; } = null;
        Comm comm { get; set; } = null;

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
          new Dictionary<string, Func<CommMessage, CommMessage>>();

        List<string> strongCompMessage = new List<string>();
        /*----< initialize server processing >-------------------------*/

        public NavigatorServer()
        {
            initializeEnvironment();
            Console.Title = "Navigator Server";
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }
        /*----< set Environment properties needed by server >----------*/

        void initializeEnvironment()
        {
            Environment.root = ServerEnvironment.root;
            Environment.address = ServerEnvironment.address;
            Environment.port = ServerEnvironment.port;
            Environment.endPoint = ServerEnvironment.endPoint;
        }
        /*----< define how each message will be processed >------------*/

        void initializeDispatcher()
        {
            TestSuiteExec();
            getTopFiles();
            getTopDirs();
            moveIntoFolderFiles();
            moveIntoFolderDirs();
            moveOutOfFolderFiles();
            moveOutOfFolderDirs();
            performDepAnalysis();
            performStrongComp();

        }

        //performs strong compoenent analysis
        void performStrongComp()
        {
            Func<CommMessage, CommMessage> performStrongComp = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "performStrongComp";
                Executive exe = new Executive();
                foreach (var item in msg.arguments)
                {
                    string path = System.IO.Path.Combine(Environment.root, item);
                    string b = System.IO.Path.GetFullPath(path);
                    exe.files.Add(b);
                }
                exe.typeAnalysis(exe.files);
                exe.dependencyAnalysis(exe.files);
                Repository repo = Repository.getInstance();
                repo.typeTable.show();
                repo.dependencyTable.show();
                Console.Write("\n\n  Building dependency graph");
                Console.Write("\n ---------------------------");

                CsGraph<string, string> graph = exe.buildDependencyGraph();
                graph.showDependencies();

                Console.Write("\n\n  Strong Components:");
                Console.Write("\n --------------------");
                graph.strongComponents();
                foreach (var item in graph.strongComp)
                {
                    Console.Write("\n  Component {0}", item.Key);
                    Console.Write("\n    ");
                    foreach (var elem in item.Value)
                    {
                        Console.Write("{0} ", elem.name);
                    }
                }
                strongCompToString(graph);
                Console.Write("\n\n");

                reply.arguments = strongCompMessage;

                return reply;
            };
            messageDispatcher["performStrongComp"] = performStrongComp;
        }

        //performs testing functions
        void TestSuiteExec()
        {
            Func<CommMessage, CommMessage> TestSuiteExec = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "TestSuiteExec";
                return reply;
            };
            messageDispatcher["TestSuiteExec"] = TestSuiteExec;
        }

        //gets the tops of server files
        void getTopFiles()
        {
            Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopFiles"] = getTopFiles;
        }

        //gets the tops of directories
        void getTopDirs()
        {
            Func<CommMessage, CommMessage> getTopDirs = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopDirs";
                reply.arguments = localFileMgr.getDirs().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopDirs"] = getTopDirs;
        }

        //moves into folders
        void moveIntoFolderFiles()
        {
            Func<CommMessage, CommMessage> moveIntoFolderFiles = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["moveIntoFolderFiles"] = moveIntoFolderFiles;
        }

        //moves into directories
        void moveIntoFolderDirs()
        {
            Func<CommMessage, CommMessage> moveIntoFolderDirs = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderDirs";
                reply.arguments = localFileMgr.getDirs().ToList<string>();
                return reply;
            };
            messageDispatcher["moveIntoFolderDirs"] = moveIntoFolderDirs;
        }

        //moves up a level for files
        void moveOutOfFolderFiles()
        {
            Func<CommMessage, CommMessage> moveOutOfFolderFiles = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveOutOfFolderFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["moveOutOfFolderFiles"] = moveOutOfFolderFiles;
        }

        //moves up a directory
        void moveOutOfFolderDirs()
        {
            Func<CommMessage, CommMessage> moveOutOfFolderDirs = (CommMessage msg) =>
            {
                if (localFileMgr.currentPath != "")
                {
                    Console.WriteLine(localFileMgr.currentPath);
                    Console.WriteLine(localFileMgr.pathStack.Peek().ToString());
                    localFileMgr.currentPath = localFileMgr.pathStack.Peek().ToString();
                    Console.WriteLine("asdf{0}", localFileMgr.currentPath);
                    Console.WriteLine("wert{0}", localFileMgr.pathStack.Pop().ToString());
                }

                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveOutOfFolderDirs";
                reply.arguments = localFileMgr.getDirs().ToList<string>();
                return reply;
            };
            messageDispatcher["moveOutOfFolderDirs"] = moveOutOfFolderDirs;
        }

        //performs dependency analysis
        void performDepAnalysis()
        {
            Func<CommMessage, CommMessage> performDepAnalysis = (CommMessage msg) =>
            {
                List<string> files = new List<string>();
                foreach (string i in msg.arguments)
                {
                    string path = Path.Combine(Environment.root, i);
                    string absPath = Path.GetFullPath(path);
                    Console.WriteLine(absPath);
                    files.Add(absPath);
                }
                List<string> resultDep = new List<string>();
                resultDep = performDependency(files);
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "performDepAnalysis";
                reply.arguments = resultDep;

                return reply;
            };
            messageDispatcher["performDepAnalysis"] = performDepAnalysis;
        }

        public List<string> performDependency(List<string> files)
        {
            Executive exec = new Executive();
            exec.typeAnalysis(files);
            Console.Write("\n  TypeTable Contents:");
            Console.Write("\n ---------------------");
            Repository repo = Repository.getInstance();
            repo.typeTable.show();
            Console.Write("\n  Dependency Analysis:");
            Console.Write("\n ----------------------");

            exec.dependencyAnalysis(files);
            repo.dependencyTable.show();
            repo.dependencyTable.depDisplayFormat();

            return repo.dependencyTable.depFormat;
        }

        void strongCompToString(CsGraph<string, string> graph)
        {
            strongCompMessage.Clear();
            foreach (var item in graph.strongComp)
            {
                string file = item.Key.ToString();
                strongCompMessage.Add(file);
                if (item.Value.Count == 0)
                {
                    strongCompMessage.Add(";");
                    continue;
                }
                strongCompMessage.Add(":");
                foreach (var elem in item.Value)
                {
                    strongCompMessage.Add(elem.name.ToString());
                    strongCompMessage.Add("\t");
                }
                strongCompMessage.Add(";");
            }
        }
        static void Main(string[] args)
        {
            TestUtilities.title("Starting Navigation Server", '=');
            try
            {
                NavigatorServer server = new NavigatorServer();
                server.initializeDispatcher();
                server.comm = new MessagePassingComm.Comm(ServerEnvironment.address, ServerEnvironment.port);

                while (true)
                {
                    CommMessage msg = server.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    CommMessage reply = server.messageDispatcher[msg.command](msg);
                    reply.show();
                    server.comm.postMessage(reply);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}
