/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using Falcon.Helpers.FReflection;

namespace Falcon.Modules.Core.Network
{
    [Serializable]
    public abstract class FMessage : IFReflection
    {
         
        public string sessionId;
        public string messageId;
        public TransportType transport = TransportType.TCP;
        public long csSequence;
        public long scSequence;
        public long timeServer;
        public long timeClient;
        public int numberClone = -1;
        public bool waiting4CameEvent = false;
        public int cameTimeout;
        public bool waiting4DoneEvent = false;
        public int doneTimeout;
        public string watingSCMessage;
        public int scTimeout;
        public bool isCompressed = false;

        public FMessage CopyAllProperties(FMessage message)
        {
            this.sessionId = message.sessionId;
            this.messageId = message.messageId;
            this.transport = message.transport;
            this.csSequence = message.csSequence;
            this.scSequence = message.scSequence;
            this.timeServer = message.timeServer;
            this.timeClient = message.timeClient;
            this.numberClone = message.numberClone;
            this.waiting4CameEvent = message.waiting4CameEvent;
            this.cameTimeout = message.cameTimeout;
            this.waiting4DoneEvent = message.waiting4DoneEvent;
            this.doneTimeout = message.doneTimeout;
            this.watingSCMessage = message.watingSCMessage;
            this.scTimeout = message.scTimeout;
            this.isCompressed = message.isCompressed;
            return this;
        }
        
        public FBinaryWriter initWriter(int size)
        {
            FBinaryWriter writer = new FBinaryWriter(size);
            writer.WriteString(sessionId);
            writer.WriteString(messageId);
            writer.WriteInt((int)transport);
            writer.WriteLong(csSequence);
            writer.WriteLong(scSequence);
            writer.WriteLong(timeServer);
            writer.WriteLong(timeClient);
            writer.WriteInt(numberClone);
            writer.WriteBool(waiting4CameEvent);
            writer.WriteInt(cameTimeout);
            writer.WriteBool(waiting4DoneEvent);
            writer.WriteInt(doneTimeout);
            writer.WriteString(watingSCMessage);
            writer.WriteInt(scTimeout);
            writer.WriteBool(isCompressed);
            return writer;
        }

        public FBinaryReader initReader(byte[] bytes)
        {
            FBinaryReader reader = new FBinaryReader(bytes);
            sessionId = reader.ReadString();
            messageId = reader.ReadString();
            transport = reader.ReadInt() == 0 ? TransportType.TCP : TransportType.UDP;
            csSequence = reader.ReadLong();
            scSequence = reader.ReadLong();
            timeServer = reader.ReadLong();
            timeClient = reader.ReadLong();
            numberClone = reader.ReadInt();
            waiting4CameEvent = reader.ReadBool();
            cameTimeout = reader.ReadInt();
            waiting4DoneEvent = reader.ReadBool();
            doneTimeout = reader.ReadInt();
            watingSCMessage = reader.ReadString();
            scTimeout = reader.ReadInt();
            isCompressed = reader.ReadBool();
            return reader;
        }
    }
}

