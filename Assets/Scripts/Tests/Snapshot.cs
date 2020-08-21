
using UnityEngine;

namespace Tests
{
    public class Snapshot
    {
        public int sequence;
        public CubeEntity cubeEntity;
        public Vector3 position;
        public Vector3 rotation;

        public Snapshot(int sequence, CubeEntity cubeEntity)
        {
            this.sequence = sequence;
            this.cubeEntity = cubeEntity;
        }
        
        public Snapshot(CubeEntity cubeEntity)
        {
            this.sequence = -1;
            this.cubeEntity = cubeEntity;
        }
        
        public void Serialize(BitBuffer buffer) {
            buffer.PutInt(sequence);
            cubeEntity.Serialize(buffer);
        }

        public void Deserialize(BitBuffer buffer) {
            sequence = buffer.GetInt();
            cubeEntity.Deserialize(buffer);
        }
    }
}