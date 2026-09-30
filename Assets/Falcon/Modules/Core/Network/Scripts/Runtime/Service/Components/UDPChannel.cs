/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Falcon;
using System.Collections;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace Falcon.Modules.Core.Network
{
    public class UDPChannel
    {
        private const string Separator = ":";
        private IPAddress _serverIp;
        private IPEndPoint _hostEndPoint;

        private IPEndPoint _localIpPort;
        private UdpClient _client;

        public UDPChannel(string serverIp, int serverPort)
        {
            _serverIp = IPAddress.Parse(serverIp);
            _hostEndPoint = new IPEndPoint(_serverIp, serverPort);

            _client = new UdpClient();
            _client.Connect(_hostEndPoint);
            _client.Client.Blocking = false;
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
            _localIpPort = new IPEndPoint(_serverIp, ((IPEndPoint)_client.Client.LocalEndPoint).Port);
            
            CoroutineRunner.Instance.StartRoutine(ReceiveMessage());
        }
        
        public void Reconnect()
        {
            try { _client?.Close(); } catch {}
            _client = new UdpClient(0);
            _client.Connect(_hostEndPoint);
            _client.Client.Blocking = false;
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
            _localIpPort = new IPEndPoint(_serverIp, 0);
        }

        IEnumerator ReceiveMessage()
        {
            const int MAX_PER_FRAME = 50;          // chỉnh theo tải
            const float IDLE_SLEEP = 0.02f;        // 20ms ~ 50fps polling khi rảnh

            WaitForSecondsRealtime idleWait = new WaitForSecondsRealtime(IDLE_SLEEP);

            while (true)
            {
                int processed = 0;

                // xử lý theo batch, không để ăn hết CPU trong 1 frame
                while (_client != null && _client.Available > 0 && processed < MAX_PER_FRAME)
                {
                    processed++;

                    try
                    {
                        var content = _client.Receive(ref _localIpPort);
                        ProcessContent(content);   // tách hàm để code sạch
                    }
                    catch (Exception ex)
                    {
                        // tối thiểu: log có rate-limit để không spam
                        // Debug.LogException(ex);
                        break; // tránh loop lỗi liên tục gây nóng CPU
                    }
                }

                if (processed == 0)
                {
                    // không có data -> ngủ một chút
                    yield return idleWait;
                }
                else
                {
                    // có data -> nhường frame cho Unity (quan trọng!)
                    yield return null;
                }
            }
        }
        
        void ProcessContent(byte[] content)
        {
            var reader = new FBinaryReader(content);
            string eventName = reader.ReadString();
            Type type = FNetManager.Instance.getSCMessageType(eventName);
            bool isCompact = reader.ReadBool();

            FMessage message;

            if (isCompact)
            {
                byte[] data = reader.ReadBytes();
                message = (FMessage)Activator.CreateInstance(type);
                ((ISCBinary)message).FromBytes(data);
            }
            else
            {
                string json = reader.ReadString();
                // TODO mức B sẽ tối ưu chỗ này (reuse decoder)
                JsonDotNetEncoder decoder = new JsonDotNetEncoder();
                message = (FMessage)decoder.Decode(json, type);
            }

            FNetManager.Instance.GetSession().OnMessage(message);
        }


        public void Send(FMessage message)
        {
            FSession session = FNetManager.Instance.GetSession();
            message.transport = TransportType.UDP;
            message.timeClient = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            message.sessionId = session.GetSessionId();
            session.maxCSSequence++;
            message.csSequence = session.maxCSSequence;
            message.scSequence = session.maxSCSequence;
            message.messageId = UUIDUtil.UUID16();
            
            FBinaryWriter writer = new FBinaryWriter(1024);
            String eventName = FAMessageAttribute.GetEventName(message.GetType());
            writer.WriteString(eventName);
            String sessionId = FNetManager.Instance.GetSession().GetSessionId();
            writer.WriteString(sessionId);
            Debug.Log("UDP: " + eventName + " " + sessionId);
            if (message is ICSBinary)
            {
                writer.WriteBool(true);
                byte[] bytes = ((ICSBinary)message).ToBytes();
                writer.WriteBytes(bytes);
            }
            else
            {
                writer.WriteBool(false);
                JsonDotNetEncoder encoder = new JsonDotNetEncoder();
                writer.WriteString(encoder.Encode(new List<object> { message }));
            }

            byte[] data = writer.ToArray();
            _client.Send(data, data.Length);
        }
    }
}