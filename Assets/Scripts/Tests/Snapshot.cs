using UnityEngine;

namespace Tests
{
    public class Snapshot
    {
        public int sequence;
        public WorldInfo  worldInfo;
        public CubeEntity cubeEntity;
        public Vector3 position;
        public Vector3 rotation;

        public Snapshot(int sequence, CubeEntity cubeEntity)
        {
            this.sequence = sequence;
            this.cubeEntity = cubeEntity;
        }
        
        public Snapshot(int sequence, CubeEntity cubeEntity, WorldInfo worldInfo)
        {
            this.sequence = sequence;
            this.cubeEntity = cubeEntity;
            this.worldInfo = worldInfo;
        }
        
        public Snapshot(CubeEntity cubeEntity)
        {
            this.sequence = -1;
            this.cubeEntity = cubeEntity;
        }
        
        public void Serialize(BitBuffer buffer) {
            // TODO replace with enum to diferentiate snapshot from ack
            buffer.PutInt(0);
            buffer.PutInt(sequence);
            worldInfo.Serialize(buffer);
//            cubeEntity.Serialize(buffer);
        }

        public void Deserialize(BitBuffer buffer) {
            sequence = buffer.GetInt();
            worldInfo = WorldInfo.Deserialize(buffer);
//            cubeEntity.Deserialize(buffer);
        }
    }
}