using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public interface IMessage
{
    bool Execute(SystemBase systemBase);
}
public interface ISingleMessage
{
    void Execute(SystemBase systemBase);
}

public class MessageQueue
{
    readonly List<IMessage> Messages = new();

    public void Add(IMessage message) { Messages.Add(message); }

    class MessageFromSingle : IMessage
    {
        readonly ISingleMessage msg;
        public MessageFromSingle(ISingleMessage msg) { this.msg = msg; }
        public bool Execute(SystemBase systemBase) { msg.Execute(systemBase); return true; }
    }
    public void Add(ISingleMessage message)  { Add(new MessageFromSingle(message)); }
    
    public void Execute(SystemBase systemBase)
    {
        for(int i=0; i<Messages.Count; i++)
        {
            bool remove = Messages[i].Execute(systemBase);
            if(remove)
            {
                Messages.RemoveAt(i);
                i--;
            }
        }

    }
}
