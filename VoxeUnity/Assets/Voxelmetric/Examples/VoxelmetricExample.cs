using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Voxelmetric.Examples
{
    public class VoxelmetricExample : MonoBehaviour
    {
        public World world;
        public Camera cam;
        private Vector2 rot;

        public string blockToPlace = "air";
        public Text selectedBlockText;
        public Text saveProgressText;

        private Vector3Int pfStart;
        private Vector3Int pfStop;
        public PathFinder pf;

        private SaveProgress saveProgress;
        private EventSystem eventSystem;

        public void SetType(string newType)
        {
            blockToPlace = newType;
        }

        void Start()
        {
            rot.y = 360f - cam.transform.localEulerAngles.x;
            rot.x = cam.transform.localEulerAngles.y;
            eventSystem = FindObjectOfType<EventSystem>();
        }

        void Update()
        {
            if (saveProgress != null && saveProgress.GetProgress() >= 100)
            {
                saveProgress = null;
            }

            // Roatation
            if (Input.GetMouseButton(1))
            {
                rot = new Vector2(
                    rot.x + Input.GetAxis("Mouse X") * 3,
                    rot.y + Input.GetAxis("Mouse Y") * 3
                    );

                cam.transform.localRotation = Quaternion.AngleAxis(rot.x, Vector3.up);
                cam.transform.localRotation *= Quaternion.AngleAxis(rot.y, Vector3.left);
            }




            // Movement
            float speedModificator = 1f;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speedModificator = 2f;
            }
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                speedModificator = 0.25f;
            }

            cam.transform.position += cam.transform.forward * 40f * speedModificator * Input.GetAxis("Vertical") * Time.deltaTime;
            cam.transform.position += cam.transform.right * 40f * speedModificator * Input.GetAxis("Horizontal") * Time.deltaTime;

            // Screenspace mouse cursor coordinates
            Vector3 mousePos = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));

            if (world != null)
            {
                Block block = world.blockProvider.GetBlock(blockToPlace);

                VmRaycastHit hit = VmRaycast.Raycast(new Ray(cam.transform.position, mousePos - cam.transform.position), world, 100, block.type == BlockProvider.AIR_TYPE);

                // Display the type of the selected block
                if (selectedBlockText != null)
                {
                    selectedBlockText.text = Voxelmetric.GetBlock(world, ref hit.vector3Int).DisplayName;
                }

                // Save current world status
                if (saveProgressText != null)
                {
                    saveProgressText.text = saveProgress != null ? SaveStatus() : "Save";
                }

                if (eventSystem != null && !eventSystem.IsPointerOverGameObject())
                {
                    if (hit.block.type != BlockProvider.AIR_TYPE)
                    {
                        bool adjacent = block.type != BlockProvider.AIR_TYPE;
                        Vector3Int blockPos = adjacent ? hit.adjacentPos : hit.vector3Int;
                        Debug.DrawLine(cam.transform.position, blockPos, Color.red);
                    }

                    // Clicking voxel blocks
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (hit.block.type != BlockProvider.AIR_TYPE)
                        {
                            bool adjacent = block.type != BlockProvider.AIR_TYPE;
                            Vector3Int blockPos = adjacent ? hit.adjacentPos : hit.vector3Int;
                            Voxelmetric.SetBlockData(world, ref blockPos, new BlockData(block.type, block.solid));
                        }
                    }

                    // Pathfinding
                    if (Input.GetKeyDown(KeyCode.I))
                    {
                        if (hit.block.type != BlockProvider.AIR_TYPE)
                        {
                            bool adjacent = block.type != BlockProvider.AIR_TYPE;
                            pfStart = adjacent ? hit.adjacentPos : hit.vector3Int;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.O))
                    {
                        if (hit.block.type != BlockProvider.AIR_TYPE)
                        {
                            bool adjacent = block.type != BlockProvider.AIR_TYPE;
                            pfStop = adjacent ? hit.adjacentPos : hit.vector3Int;
                        }
                    }

                    if (Input.GetKeyDown(KeyCode.P))
                    {
                        pf = new PathFinder(pfStart, pfStop, world, 0);
                    }

                    if (pf != null && pf.path.Count != 0)
                    {
                        for (int i = 0; i < pf.path.Count - 1; i++)
                        {
                            Vector3 p0 = (Vector3)pf.path[i] + Env.halfBlockOffset;
                            Vector3 p1 = (Vector3)pf.path[i + 1] + Env.halfBlockOffset;
                            Debug.DrawLine(p0, p1, Color.red);
                        }
                    }
                }

                // Test of ranged block setting
                if (Input.GetKeyDown(KeyCode.T))
                {
                    Action<ModifyBlockContext> action = context => { Debug.Log("Action performed"); };

                    Vector3Int fromPos = new Vector3Int(-44, -44, -44);
                    Vector3Int toPos = new Vector3Int(44, 44, 44);
                    Voxelmetric.SetBlockData(world, fromPos, toPos, BlockProvider.airBlock, action);
                }
            }
        }

        public void SaveAll()
        {
            if (saveProgress != null)
            {
                return;
            }

            saveProgress = new SaveProgress(Voxelmetric.SaveAll(world));
        }

        public string SaveStatus()
        {
            if (saveProgress == null)
            {
                return string.Empty;
            }

            return saveProgress.GetProgress().ToString() + "%";
        }
    }
}
