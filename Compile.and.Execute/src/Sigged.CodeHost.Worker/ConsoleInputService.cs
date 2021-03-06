﻿using ProtoBuf;
using Sigged.CodeHost.Core.Dto;
using Sigged.CodeHost.Core.Logging;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Sigged.CodeHost.Worker
{
    public class ConsoleInputService : TextReader
    {
        protected string sessionid;
        protected TcpClient client;
        protected Stream networkStream;

        public string receivedInput = null;

        private static Queue<ExecutionStateDto> unfinishedRequests = new Queue<ExecutionStateDto>();
        private static bool isWaitingForInput = false;

        public ConsoleInputService(string sessionid, TcpClient client)
        {
            this.sessionid = sessionid;
            this.client = client;
            this.networkStream = client.GetStream();
        }

        public override int Read()
        {
            var execState = new ExecutionStateDto
            {
                SessionId = sessionid,
                State = RemoteAppState.WaitForInput
            };
            networkStream.WriteByte((byte)MessageType.WorkerExecutionState);
            Serializer.SerializeWithLengthPrefix(networkStream, execState, PrefixStyle.Fixed32);
            Logger.LogLine($"CLIENT: sent remote inputchar request (unfinished queue: {unfinishedRequests.Count})");

            unfinishedRequests.Enqueue(execState);
            isWaitingForInput = true;

            while (isWaitingForInput)
            {
                if (client.Available > 0)
                {
                    byte msgHeader = (byte)networkStream.ReadByte();
                    MessageType msgType = (MessageType)msgHeader;

                    unfinishedRequests.Dequeue();
                    isWaitingForInput = false;

                    if (msgType == MessageType.ServerRemoteInput)
                    {
                        var remoteInput = Serializer.DeserializeWithLengthPrefix<RemoteInputDto>(networkStream, PrefixStyle.Fixed32);
                        receivedInput = remoteInput.Input;

                        Logger.LogLine($"CLIENT: received remote input {receivedInput} of length {receivedInput.Length}");
                    }
                    else
                    {
                        Logger.LogLine($"CLIENT: expected msgtype {MessageType.ServerRemoteInput} but got {msgType}");
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            Logger.LogLine($"CLIENT: returning remote input \"{receivedInput}\" of length {receivedInput.Length} to execution flow");
            return receivedInput[0];
        }

        public override string ReadLine()
        {
            try
            {
                var execState = new ExecutionStateDto
                {
                    SessionId = sessionid,
                    State = RemoteAppState.WaitForInputLine
                };
                networkStream.WriteByte((byte)MessageType.WorkerExecutionState);
                Serializer.SerializeWithLengthPrefix(networkStream, execState, PrefixStyle.Fixed32);
                Logger.LogLine($"CLIENT: sent remote inputline request (unfinished queue: {unfinishedRequests.Count})");

                unfinishedRequests.Enqueue(execState);
                isWaitingForInput = true;

                while (isWaitingForInput)
                {
                    if (client.Available > 0)
                    {
                        byte msgHeader = (byte)networkStream.ReadByte();
                        MessageType msgType = (MessageType)msgHeader;

                        unfinishedRequests.Dequeue();
                        isWaitingForInput = false;

                        if (msgType == MessageType.ServerRemoteInput)
                        {
                            var remoteInput = Serializer.DeserializeWithLengthPrefix<RemoteInputDto>(networkStream, PrefixStyle.Fixed32);
                            receivedInput = remoteInput.Input;

                            Logger.LogLine($"CLIENT: received remote input {receivedInput} of length {receivedInput.Length}");
                        }
                        else
                        {


                            Logger.LogLine($"CLIENT: expected msgtype {MessageType.ServerRemoteInput} but got {msgType}");
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                Logger.LogLine($"CLIENT: returning remote input \"{receivedInput}\" of length {receivedInput.Length} to execution flow");
                return receivedInput;
            }
            finally
            {
                receivedInput = null;
            }
        }

        public override int Read(char[] buffer, int index, int count)
        {
            buffer = new char[count];
            return 0;
        }



    }
}
