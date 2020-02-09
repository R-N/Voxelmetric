using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace Voxelmetric
{
    public static class Serialization
    {
        private static string saveLocation;
        private static string tempSaveLocation;

        private static bool isInitialized;

        public static string SaveLocation { get { return saveLocation; } set { saveLocation = value; } }
        public static string TempSaveLocation { get { return tempSaveLocation; } set { tempSaveLocation = value; } }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            saveLocation = string.Empty;
            tempSaveLocation = string.Empty;
            isInitialized = false;
        }
#endif

        public static void Initialize(string saveLocation, string tempSaveLocation)
        {
            Serialization.saveLocation = saveLocation;
            Serialization.tempSaveLocation = tempSaveLocation;

            isInitialized = true;
        }

        private static string GetSaveLocation(bool temporary)
        {
            string saveLocation = (temporary ? tempSaveLocation : Serialization.saveLocation) + "/";

            if (!Directory.Exists(saveLocation))
            {
                Directory.CreateDirectory(saveLocation);
            }

            return saveLocation;
        }

        private static string FileName(Vector3Int chunkLocation)
        {
            string fileName = chunkLocation.x + "," + chunkLocation.y + "," + chunkLocation.z + ".bin";
            return fileName;
        }

        private static string SaveFileName(Chunk chunk, bool temporary)
        {
            string saveFile = GetSaveLocation(temporary);
            saveFile += FileName(chunk.Pos);
            return saveFile;
        }

        public static bool Write(Save save, bool temporary)
        {
            Assert.IsTrue(isInitialized == true, "You must call Initialize first!");

            string path = SaveFileName(save.Chunk, temporary);
            return save.IsBinarizeNecessary() && FileHelpers.BinarizeToFile(path, save);
        }

        public static bool Read(Save save, bool temporary)
        {
            Assert.IsTrue(isInitialized == true, "You must call Initialize first!");

            string path = SaveFileName(save.Chunk, temporary);
            return FileHelpers.DebinarizeFromFile(path, save);
        }

        public static void ClearTemporary()
        {
            if (!Directory.Exists(tempSaveLocation))
            {
                return;
            }

            string[] files = Directory.GetFiles(tempSaveLocation, "*.bin");
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }

        public static void CopyFromSaveLocationToTemp()
        {
            if (!Directory.Exists(saveLocation))
            {
                return;
            }

            if (!Directory.Exists(tempSaveLocation))
            {
                Directory.CreateDirectory(tempSaveLocation);
            }

            string[] files = Directory.GetFiles(saveLocation, "*.bin");
            for (int i = 0; i < files.Length; i++)
            {
                File.Copy(files[i], tempSaveLocation + "/" + Path.GetFileName(files[i]));
            }
        }
    }
}
