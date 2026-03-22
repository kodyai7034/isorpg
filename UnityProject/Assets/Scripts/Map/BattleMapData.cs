using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    [System.Serializable]
    public struct SpawnZone
    {
        public int team;
        public Vector2Int[] tiles;
    }

    [CreateAssetMenu(fileName = "NewMap", menuName = "IsoRPG/BattleMap")]
    public class BattleMapData : ScriptableObject
    {
        public string mapName;
        public int width = 8;
        public int height = 8;
        public TileData[] tiles;
        public SpawnZone[] spawnZones;

        public TileData GetTile(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return default;
            return tiles[y * width + x];
        }

        public TileData GetTile(Vector2Int pos)
        {
            return GetTile(pos.x, pos.y);
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool InBounds(Vector2Int pos)
        {
            return InBounds(pos.x, pos.y);
        }
    }
}
