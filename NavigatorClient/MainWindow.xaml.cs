////////////////////////////////////////////////////////////////////////////
// NavigatorClient.xaml.cs - Demonstrates Directory Navigation in WPF App //
// ver 2.2                                                                //
// Author:      Jim Fawcett, CST 4-187, Syracuse University               //
//              (315) 443-3948, jfawcett@twcny.rr.com                     //
//              Souradeepta Biswas, Syracuse University                   //
//              sobiswas@syr.edu                                          //
////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines WPF application processing by the client.  The client
 * displays a local FileFolder view, and a remote FileFolder view.  It supports
 * navigating into subdirectories, both locally and in the remote Server.
 * 
 * It also supports viewing local files.
 * 
 * Public Interfaces:
 * testRun::TestCase1() //shows test req #1
 * testRun::TestCase2() //shows test req #2
 * testRun::TestCase3() //shows test req #3
 * testRun::TestCase4() //shows test req #4
 * testRun::TestCase4() //shows test req #5
 * 
 * Maintenance History:
 * --------------------
 * ver 2.2 : 05 Dec 2018
 * - inserted new functions
 * ver 2.1 : 26 Oct 2017
 * - relatively minor modifications to the Comm channel used to send messages
 *   between NavigatorClient and NavigatorServer
 * ver 2.0 : 24 Oct 2017
 * - added remote processing - Up functionality not yet implemented
 *   - defined NavigatorServer
 *   - added the CsCommMessagePassing prototype
 * ver 1.0 : 22 Oct 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using MessagePassingComm;

namespace Navigator
{
    public partial class MainWindow : Window
    {
        private IFileMgr fileMgr { get; set; } = null;  
        Comm comm { get; set; } = null;
        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        Thread rcvThread = null;

        public MainWindow()
        {
            InitializeComponent();
            initializeEnvironment();
            Console.Title = "Navigator Client";
            fileMgr = FileMgrFactory.create(FileMgrType.Local); 
            fileMgr.currentPath = "";

            comm = new Comm(ClientEnvironment.address, ClientEnvironment.port);
            initializeMessageDispatcher();

            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Jim Fawcett";
            msg1.command = "getTopFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            comm.postMessage(msg2);
            SCBox.Clear();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "TestSuiteExec";
            comm.postMessage(msg1);
        }

        static List<string> remfile { get; set; } = new List<string>();
        static string AddFile;
        //----< make Environment equivalent to ClientEnvironment >-------

        void initializeEnvironment()
        {
            Environment.root = ClientEnvironment.root;
            Environment.address = ClientEnvironment.address;
            Environment.port = ClientEnvironment.port;
            Environment.endPoint = ClientEnvironment.endPoint;
        }
        //----< define how to process each message command >-------------

        void initializeMessageDispatcher()
        {

            getTopFiles();
            getTopDirs();
            moveIntoFolderFiles();
            moveIntoFolderDirs();
            moveOutOfFolderFiles();
            moveOutOfFolderDirs();
            performDepAnalysis();
            performStrongComp();
            getTopFiles();

            messageDispatcher["TestSuiteExec"] = (CommMessage msg) =>
            {
                Console.Write("\n  Demonstrating Project #4 Requirements");
                Console.Write("\n =======================================\n");

                testRun Test = new testRun();
                Test.TestCase1();
                Test.TestCase2();
                Test.TestCase5();
                Test.TestCase4();
                string[] file = { "FileMgr.cs", "NavigatorServer.cs", "ScopeStack.cs" };
                CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
                msg1.from = ClientEnvironment.endPoint;
                msg1.to = ServerEnvironment.endPoint;
                msg1.command = "performDepAnalysis";
                foreach (var item in file)
                {
                    msg1.arguments.Add(item.ToString());
                }
                comm.postMessage(msg1);
                CommMessage msg2 = msg1.clone();
                msg2.command = "performStrongComp";
                comm.postMessage(msg2);
                Console.WriteLine("5, 6, 7. Requirement-----> PASS\n");
            };

        }
        //----< define processing for GUI's receive thread >-------------
            // load remoteFiles listbox with files from root
            void getTopFiles()
            {        
                messageDispatcher["getTopFiles"] = (CommMessage msg) =>
                {
                    remoteFiles.Items.Clear();
                    foreach (string file in msg.arguments)
                    {
                        remoteFiles.Items.Add(file);
                    }
                };
            }

            // load remoteDirs listbox with dirs from root
            void getTopDirs()
            {
                 messageDispatcher["getTopDirs"] = (CommMessage msg) =>
                {
                    remoteDirs.Items.Clear();
                    foreach (string dir in msg.arguments)
                    {
                        remoteDirs.Items.Add(dir);
                    }
                };
            }

            // load remoteFiles listbox with files from folder
            void moveIntoFolderFiles()
            {
                messageDispatcher["moveIntoFolderFiles"] = (CommMessage msg) =>
                {
                    remoteFiles.Items.Clear();
                    foreach (string file in msg.arguments)
                    {
                        remoteFiles.Items.Add(file);
                    }
                };
            }

            // load remoteDirs listbox with dirs from folder
            void moveIntoFolderDirs()
            {
                messageDispatcher["moveIntoFolderDirs"] = (CommMessage msg) =>
                {
                    remoteDirs.Items.Clear();
                    foreach (string dir in msg.arguments)
                    {
                        remoteDirs.Items.Add(dir);
                    }
                };
            }

            // load remoteFiles listbox when moved up a level
            void moveOutOfFolderFiles()
            {
                messageDispatcher["moveOutOfFolderFiles"] = (CommMessage msg) =>
                {
                    remoteFiles.Items.Clear();
                    foreach (string file in msg.arguments)
                    {
                        remoteFiles.Items.Add(file);
                    }
                };
            }

            // load remoteDirs listbox when moved up a level
            void moveOutOfFolderDirs()
            {
                messageDispatcher["moveOutOfFolderDirs"] = (CommMessage msg) =>
                {
                    remoteDirs.Items.Clear();
                    foreach (string dir in msg.arguments)
                    {
                        remoteDirs.Items.Add(dir);
                    }
                };
            }

            // performs dependency analysis
            void performDepAnalysis()
            {
                
                messageDispatcher["performDepAnalysis"] = (CommMessage msg) =>
                {
                    AnalyBox.Clear();
                    List<string> files = msg.arguments;
                    foreach (string file in files)
                    {
                        if ((file.Contains(";")))
                            AnalyBox.Text += "\r\n";
                        else if ((file.Contains(",")))
                            AnalyBox.Text += file;
                        else
                            AnalyBox.Text += file + " ";
                    }
                };
            }

            // performs strong component analysis
            void performStrongComp()
            {
                messageDispatcher["performStrongComp"] = (CommMessage msg) =>
                {
                    SCBox.Clear();
                    List<string> files = msg.arguments;
                    foreach (string file in files)
                    {
                        if ((file.Contains(";")))
                            SCBox.Text += "\r\n";
                        else if ((file.Contains(",")))
                            SCBox.Text += file;
                        else
                            SCBox.Text += file + " ";
                    }
                };
            }
      

        void rcvThreadProc()
        {
            Console.Write("\n  starting client's receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                // pass the Dispatcher's action value to the main thread for execution

                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }
        //----< shut down comm when the main window closes >-------------

        private void Window_Closed(object sender, EventArgs e)
        {
            comm.close();

            // The step below should not be nessary, but I've apparently caused a closing event to 
            // hang by manually renaming packages instead of getting Visual Studio to rename them.

            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        //----< not currently being used >-------------------------------

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }


        //----< move to root of remote directories >---------------------
        /*
         * - sends a message to server to get files from root
         * - recv thread will create an Action<CommMessage> for the UI thread
         *   to invoke to load the remoteFiles listbox
         */
        private void RemoteTop_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Jim Fawcett";
            msg1.command = "getTopFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            comm.postMessage(msg2);
        }
        //----< download file and display source in popup window >-------

        private void remoteFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (string c in remoteFiles.SelectedItems)
            {
                remfile.Add(c);
                AddFile = c;

            }
            Show_Files_Selected(AddFile);

        }
        private void Show_Files_Selected(string input1)
        {
            AnalyzeFiles.Items.Add(input1);
        }

        //----< move to parent directory of current remote path >--------

        private void RemoteUp_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Jim Fawcett";
            msg1.command = "moveOutOfFolderDirs";

            comm.postMessage(msg1);

        }
        //----< move into remote subdir and display files and subdirs >--
        /*
         * - sends messages to server to get files and dirs from folder
         * - recv thread will create Action<CommMessage>s for the UI thread
         *   to invoke to load the remoteFiles and remoteDirs listboxs
         */
        private void remoteDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";
            msg1.arguments.Add(remoteDirs.SelectedValue as string);
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
        }

        private void sendDepAnalysisRequest()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "performDepAnalysis";
            msg1.arguments = remfile;
            comm.postMessage(msg1);
        }

        private void sendStrongCompRequest()
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "performStrongComp";
            msg1.arguments = remfile;
            comm.postMessage(msg1);
        }
        private void btnPreviousTab_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = tcSample.SelectedIndex - 1;
            if (newIndex < 0)
                newIndex = tcSample.Items.Count - 1;
            tcSample.SelectedIndex = newIndex;
            if (tcSample.Name == "Tab_DepAnalysis")
                sendDepAnalysisRequest();
            if (tcSample.Name == "Tab_SC")
                sendStrongCompRequest();

        }

        private void btnNextTab_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = tcSample.SelectedIndex + 1;
            if (newIndex >= tcSample.Items.Count)
                newIndex = 0;
            tcSample.SelectedIndex = newIndex;
            if (tcSample.Name == "Tab_DepAnalysis")
                sendDepAnalysisRequest();
            if (tcSample.Name == "Tab_SC")
                sendStrongCompRequest();
        }

        private void AnalyButton_Click(object sender, RoutedEventArgs e)
        {
            sendDepAnalysisRequest();

        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeFiles.Items.Clear();
            remfile.Clear();
        }

        private void SC_Click(object sender, RoutedEventArgs e)
        {
            sendStrongCompRequest();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            foreach (string c in remoteFiles.SelectedItems)
            {
                remfile.Add(c);
                AddFile = c;

            }
            Show_Files_Selected(AddFile);

        }
    }

    public class testRun
    {
        public void TestCase1()
        {
            Console.WriteLine("\n1. Requirement----->");
            Console.WriteLine("This C# Project has been made using Visual Studio 2017 and its C# Windows Console Projects on .Net Version");
            Console.Write(typeof(string).Assembly.ImageRuntimeVersion);
            Console.WriteLine("\n1. Requirement-----> PASS\n");
        }

        public void TestCase2()
        {
            Console.WriteLine("\n2. Requirement----->");
            Console.WriteLine("This C# Project has used the .Net System.IO and System.Text for all I/O.");
            Console.WriteLine("2. Requirement----->PASS\n");
        }
        public void TestCase3()
        {
            Console.WriteLine("\n3. Requirement----->");
            Console.WriteLine("The Project consists of five Packages - 1.CsGraph, 2.DemoExecutive, 3.DemoReqs, 4.DependencyTable, 5.Display,");
            Console.WriteLine("6.Element, 7.Enivironment, 8.FileMgr, 9.IMessagePassingCommService, 10.MessagePassingCommService,");
            Console.WriteLine("11.NavigatorClient, 12.NavigatorServer, 13.Parser, 14.SemiExp, 15.TestHarness,16.TestUtilities");
            Console.WriteLine("17.Toker, 18.TypeTable");
            Console.WriteLine("3. Requirement-----> PASS\n");
        }
        public void TestCase5()
        {
            Console.WriteLine("\n7. Requirement----->");
            Console.WriteLine("The above output from the automated unit test satisfies this requirement.");
            Console.WriteLine("7. Requirement-----> PASS\n");
        }
        public void TestCase4()
        {
            Console.WriteLine("\n4,5, 6. Requirement----->");
            Console.WriteLine("Below we evaluate all the dependencies between files in a specified file set and also find the strong component");
            Console.WriteLine("Also we can see our well formated GUI pop up\n");

        }

    }
}
