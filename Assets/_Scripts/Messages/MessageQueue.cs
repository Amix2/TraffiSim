using System.Collections.Generic;

public interface IMessage
{
    bool Execute(MasterSystem masterSystem);
}

public interface ISingleMessage
{
    void Execute(MasterSystem masterSystem);
}

public class MessageQueue
{
    private readonly List<IMessage> Messages = new();

    public void Add(IMessage message)
    { Messages.Add(message); }

    private class MessageFromSingle : IMessage
    {
        private readonly ISingleMessage msg;

        public MessageFromSingle(ISingleMessage msg)
        { this.msg = msg; }

        public bool Execute(MasterSystem masterSystem)
        { msg.Execute(masterSystem); return true; }
    }

    public void Add(ISingleMessage message)
    { Add(new MessageFromSingle(message)); }

    public void Execute(MasterSystem masterSystem)
    {
        for (int i = 0; i < Messages.Count; i++)
        {
            bool remove = Messages[i].Execute(masterSystem);
            if (remove)
            {
                Messages.RemoveAt(i);
                i--;
            }
        }
    }
}