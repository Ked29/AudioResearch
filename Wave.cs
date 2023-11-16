using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AudioResearch
{
    public class Wave
    {
        public struct WAVEHEADER
        {
            public string ChunkID; // RIFF
            public uint ChunkSize; // File Size
            public string Format; // WAVE
            public string Subchunk1ID; //fmt
            public uint Subchunk1Size; //
            public ushort AudioFormat;
            public ushort NumChannels;
            public uint SampleRate;
            public uint ByteRate;
            public ushort BlockAlign;
            public ushort BitsPerSample;
            public string Subchunk2ID;
            public uint Subchunk2Size; //audio data
        }

        public WAVEHEADER Header;
        public byte[] AudioData;
        private SoundPlayer SoundPlayer { get; set; }

        static byte[] ReadAudioData(BinaryReader reader, int dataSize)
        {
            byte[] audioData = reader.ReadBytes(dataSize);
            return audioData;
        }

        static WAVEHEADER ReadWavHeader(BinaryReader reader)
        {
            WAVEHEADER header = new WAVEHEADER();
            header.ChunkID = Encoding.ASCII.GetString(reader.ReadBytes(4));
            header.ChunkSize = reader.ReadUInt32();
            header.Format = Encoding.ASCII.GetString(reader.ReadBytes(4));
            header.Subchunk1ID = Encoding.ASCII.GetString(reader.ReadBytes(4));
            header.Subchunk1Size = reader.ReadUInt32();
            header.AudioFormat = reader.ReadUInt16();
            header.NumChannels = reader.ReadUInt16();
            header.SampleRate = reader.ReadUInt32();
            header.ByteRate = reader.ReadUInt32();
            header.BlockAlign = reader.ReadUInt16();
            header.BitsPerSample = reader.ReadUInt16();
            header.Subchunk2ID = Encoding.ASCII.GetString(reader.ReadBytes(4));
            //this was a headache since I wasn't performing the check before so Subchunk2ID would return LIST and Subchunk2Size would be tiny since it was the list chunk size which is 26, so it led me to do research on this
            // Check for the presence of "LIST" chunk cause like a lot of modern companies add their copyright information here which in a standard wav would be the "DATA" chunk at position 44
            if (header.Subchunk2ID == "LIST")
            {
                // Skip the "LIST" chunk content and move to 74 bytes where you'll now get "DATA" chunk
                long listChunkSize = reader.ReadUInt32();
                reader.BaseStream.Seek(listChunkSize, SeekOrigin.Current);

                // Read the next subchunk ID (should be "data") and from here we read next 4 bytes which will give us the actual size of the raw audio data
                header.Subchunk2ID = Encoding.ASCII.GetString(reader.ReadBytes(4));
            }

            header.Subchunk2Size = reader.ReadUInt32();

            return header;
        }



        public Wave()
        { 
            Header = new WAVEHEADER();
        }
        public Wave(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        Header = ReadWavHeader(reader);
                        AudioData = ReadAudioData(reader, (int)Header.Subchunk2Size);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void Play()
        {
            using (MemoryStream stream = new MemoryStream(GenerateWavStructure()))
            {
                SoundPlayer = new SoundPlayer(stream);
                SoundPlayer.Play();
            }
        }
        public void Stop()
        {
            SoundPlayer.Stop();
        }
        private byte[] GenerateWavHeaderBytes()
        {
            List<byte> headerBytes = new List<byte>();
            headerBytes.AddRange(Encoding.ASCII.GetBytes(Header.ChunkID));
            headerBytes.AddRange(BitConverter.GetBytes(Header.ChunkSize));
            headerBytes.AddRange(Encoding.ASCII.GetBytes(Header.Format));
            headerBytes.AddRange(Encoding.ASCII.GetBytes(Header.Subchunk1ID));
            headerBytes.AddRange(BitConverter.GetBytes(Header.Subchunk1Size));
            headerBytes.AddRange(BitConverter.GetBytes(Header.AudioFormat));
            headerBytes.AddRange(BitConverter.GetBytes(Header.NumChannels));
            headerBytes.AddRange(BitConverter.GetBytes(Header.SampleRate));
            headerBytes.AddRange(BitConverter.GetBytes(Header.ByteRate));
            headerBytes.AddRange(BitConverter.GetBytes(Header.BlockAlign));
            headerBytes.AddRange(BitConverter.GetBytes(Header.BitsPerSample));
            headerBytes.AddRange(Encoding.ASCII.GetBytes(Header.Subchunk2ID));
            headerBytes.AddRange(BitConverter.GetBytes(Header.Subchunk2Size));
            return headerBytes.ToArray();
        }

        private byte[] GenerateWavStructure()
        {
            byte[] header = GenerateWavHeaderBytes();
            byte[] audioData = AudioData;
            byte[] wav = header.Concat(audioData).ToArray();
            return wav;
        }
    }
}
