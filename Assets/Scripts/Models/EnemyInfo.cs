using UnityEngine;

public class EnemyInfo : MonoBehaviour
{
        private int id;
        [SerializeField]
        private GameObject enemyGameObject;

        public void SetId(int id)
        {
            this.id = id;
        }

        public int GetId()
        {
            return id;
        }

        public void DestroyGameObject()
        {
            Destroy(enemyGameObject);
        }
}
