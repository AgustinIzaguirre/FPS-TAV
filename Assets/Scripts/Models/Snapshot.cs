using UnityEngine;

namespace Tests
{
    public class Snapshot
    {
        public int sequence;
        public WorldInfo  worldInfo;

        public Snapshot(int sequence, WorldInfo worldInfo)
        {
            this.sequence = sequence;
            this.worldInfo = worldInfo;
        }
        
        public Snapshot()
        {
            this.sequence = -1;
        }
        
        public void Serialize(BitBuffer buffer) {
            // TODO replace with enum to diferentiate snapshot from ack
            buffer.PutInt((int) PacketType.SNAPSHOT);
            buffer.PutInt(sequence);
            worldInfo.Serialize(buffer);
        }

        public void Deserialize(BitBuffer buffer) {
            sequence = buffer.GetInt();
            worldInfo = WorldInfo.Deserialize(buffer);
        }
    }
}