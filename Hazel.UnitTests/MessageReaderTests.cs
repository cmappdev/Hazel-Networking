﻿using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hazel.UnitTests
{
    [TestClass]
    public class MessageReaderTests
    {
        [TestMethod]
        public void ReadProperInt()
        {
            const int Test1 = int.MaxValue;
            const int Test2 = int.MinValue;

            var msg = new MessageWriter(128);
            msg.StartMessage(1);
            msg.Write(Test1);
            msg.Write(Test2);
            msg.EndMessage();

            Assert.AreEqual(11, msg.Length);
            Assert.AreEqual(msg.Length, msg.Position);

            MessageReader reader = MessageReader.Get(msg.Buffer, 0);
            Assert.AreEqual(Test1, reader.ReadInt32());
            Assert.AreEqual(Test2, reader.ReadInt32());
        }

        [TestMethod]
        public void ReadProperBool()
        {
            const bool Test1 = true;
            const bool Test2 = false;

            var msg = new MessageWriter(128);
            msg.StartMessage(1);
            msg.Write(Test1);
            msg.Write(Test2);
            msg.EndMessage();

            Assert.AreEqual(5, msg.Length);
            Assert.AreEqual(msg.Length, msg.Position);

            MessageReader reader = MessageReader.Get(msg.Buffer, 0);

            Assert.AreEqual(Test1, reader.ReadBoolean());
            Assert.AreEqual(Test2, reader.ReadBoolean());

        }

        [TestMethod]
        public void ReadProperString()
        {
            const string Test1 = "Hello";
            string Test2 = new string(' ', 1024);
            var msg = new MessageWriter(2048);
            msg.StartMessage(1);
            msg.Write(Test1);
            msg.Write(Test2);
            msg.Write(string.Empty);
            msg.EndMessage();

            Assert.AreEqual(msg.Length, msg.Position);

            MessageReader reader = MessageReader.Get(msg.Buffer, 0);

            Assert.AreEqual(Test1, reader.ReadString());
            Assert.AreEqual(Test2, reader.ReadString());
            Assert.AreEqual(string.Empty, reader.ReadString());

        }

        [TestMethod]
        public void ReadProperFloat()
        {
            const float Test1 = 12.34f;

            var msg = new MessageWriter(2048);
            msg.StartMessage(1);
            msg.Write(Test1);
            msg.EndMessage();

            Assert.AreEqual(7, msg.Length);
            Assert.AreEqual(msg.Length, msg.Position);

            MessageReader reader = MessageReader.Get(msg.Buffer, 0);

            Assert.AreEqual(Test1, reader.ReadSingle());

        }

        [TestMethod]
        public void ReadMessageLength()
        {
            var msg = new MessageWriter(2048);
            msg.StartMessage(1);
            msg.Write(65534);
            msg.StartMessage(2);
            msg.Write("HO");
            msg.EndMessage();
            msg.StartMessage(2);
            msg.Write("NO");
            msg.EndMessage();
            msg.EndMessage();

            Assert.AreEqual(msg.Length, msg.Position);

            MessageReader reader = MessageReader.Get(msg.Buffer, 0);
            Assert.AreEqual(1, reader.Tag);
            Assert.AreEqual(65534, reader.ReadInt32()); // Content

            var sub = reader.ReadMessage();
            Assert.AreEqual(3, sub.Length);
            Assert.AreEqual(2, sub.Tag);
            Assert.AreEqual("HO", sub.ReadString());

            sub = reader.ReadMessage();
            Assert.AreEqual(3, sub.Length);
            Assert.AreEqual(2, sub.Tag);
            Assert.AreEqual("NO", sub.ReadString());
        }

        [TestMethod]
        public void TestMessage()
        {
            string test = "5 32 0 0 0 5 0 5 255 255 255 255 15 2 0 2 2 2 9 0 1 4 110 123 233 131 255 127 255 127";
            byte[] testValues = test.Replace("-", "").Split(' ').Select(b => byte.Parse(b)).ToArray();
            
            MessageReader msg = MessageReader.Get(testValues, 0, testValues.Length);

            msg.ReadInt32();
            msg.ReadByte();

            while (msg.Position < msg.Length)
            {
                var sub = msg.ReadMessage();
                Console.WriteLine($"Position: {msg.Position}/{msg.Length}");

                if (sub.Tag == 4) // Spawn
                {
                    uint spawnId = sub.ReadPackedUInt32();
                    if (spawnId == 4)
                    {
                        int ownerId = sub.ReadPackedInt32();
                        int numChild = sub.ReadPackedInt32();
                        Console.WriteLine($"Spawning {spawnId} for {ownerId} with {numChild} children");
                        for (int i = 0; i < numChild; ++i)
                        {
                            uint childId = sub.ReadPackedUInt32();
                            var childReader = sub.ReadMessage();
                            Console.WriteLine($"Child {childId} has data ({childReader.Tag}) len={childReader.Length}");
                        }
                    }
                    else if (spawnId == 3)
                    {
                        int ownerId = sub.ReadPackedInt32();
                        int numChild = sub.ReadPackedInt32();
                        Console.WriteLine($"Spawning {spawnId} for {ownerId} with {numChild} children");
                        for (int i = 0; i < numChild; ++i)
                        {
                            uint childId = sub.ReadPackedUInt32();
                            var childReader = sub.ReadMessage();
                            
                            var gameGuid = new Guid(childReader.ReadBytesAndSize());
                            var numPlayers = childReader.ReadByte();
                            Console.WriteLine($"Child {childId} has data: {gameGuid} NumPlayers= {numPlayers}");
                            Console.WriteLine($"Remainder Data = {string.Join(" ", childReader.ReadBytes(childReader.Length - childReader.Position))}");
                            
                        }
                    }
                    else
                    {
                        sub.Position = 0;
                        Console.WriteLine($"Tag: {sub.Tag}\tLength: {sub.Length}\tData = {string.Join(" ", sub.ReadBytes(sub.Length).Select(s => s.ToString()).ToArray())}");
                    }
                }
                else
                {
                    sub.Position = 0;
                    Console.WriteLine($"Tag: {sub.Tag}\tLength: {sub.Length}\tData = {string.Join(" ", sub.ReadBytes(sub.Length).Select(s => s.ToString()).ToArray())}");
                }

            }
        }

        [TestMethod]
        public void GetLittleEndian()
        {
            Assert.IsTrue(MessageWriter.IsLittleEndian());
        }

        [TestMethod]
        public void Test()
        {
            sbyte s = -1;
            Assert.AreEqual(255, (byte)s);
            byte b = 255;
            Assert.AreEqual(-1, (sbyte)b);
        }
    }
}