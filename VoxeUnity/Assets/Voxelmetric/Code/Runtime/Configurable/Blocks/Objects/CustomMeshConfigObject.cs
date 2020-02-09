using UnityEngine;

namespace Voxelmetric
{
    [CreateAssetMenu(fileName = "New Custom Mesh", menuName = "Voxelmetric/Blocks/Custom Mesh")]
    public class CustomMeshConfigObject : BlockConfigObject
    {
        [SerializeField]
        private GameObject meshObject = null;
        [SerializeField]
        private Texture2D texture = null;
        [SerializeField]
        private Vector3 meshOffset = Vector3.zero;

        public GameObject MeshObject { get { return meshObject; } set { meshObject = value; } }
        public Texture2D Texture { get { return texture; } set { texture = value; } }
        public Vector3 MeshOffset { get { return meshOffset; } set { meshOffset = value; } }

        public override object GetBlockClass()
        {
            return typeof(CustomMeshBlock);
        }

        public override BlockConfig GetConfig()
        {
            if (meshObject == null)
            {
                Debug.LogError("Mesh Object can't be null on " + name);
                return null;
            }

            return new CustomMeshBlockConfig()
            {
                RaycastHit = true,
                RaycastHitOnRemoval = true,
            };
        }
    }
}