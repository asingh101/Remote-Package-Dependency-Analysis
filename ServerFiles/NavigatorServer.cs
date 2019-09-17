// NavigationServer

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePassingComm;

namespace Navigator
{
  public class NavigatorServer
  {
    IFileMgr localFileMgr { get; set; } = null;
    Comm comm { get; set; } = null;
    Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = new Dictionary<string, Func<CommMessage, CommMessage>>();
    public NavigatorServer()
    {
      initializeEnvironment();
      localFileMgr = FileMgrFactory.create(FileMgrType.Local);
    }

    void initializeEnvironment()
    {
      Environment.root = ServerEnvironment.root;
      Environment.address = ServerEnvironment.address;
      Environment.port = ServerEnvironment.port;
      Environment.endPoint = ServerEnvironment.endPoint;
    }
    CommMessage initializeReply(CommMessage msg)
    {
      CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
      reply.to = msg.from;
      reply.from = msg.to;
      reply.command = msg.command;
      return reply;
    }
    void initializeDispatcher()
    {
      Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>
      {
        localFileMgr.currentPath = "";
        CommMessage reply = initializeReply(msg);
        reply.arguments = localFileMgr.getFiles().ToList<string>();
        return reply;
      };
      messageDispatcher["getTopFiles"] = getTopFiles;
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
          server.comm.postMessage(reply);
        }
      }
      catch(Exception ex)
      {
        Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
      }
    }
  }
}
