using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    [System.Serializable]
    public struct TileData
    {
        public Vector2Int position;
        public int elevation;
        public TerrainType terrain;
        public bool walkable;
        public int moveCost;
        public int cover;
        public HazardType hazard;

        public TileData(int x, int y, int elevation = 0, TerrainType terrain = TerrainType.Grass)
        {
            position = new Vector2Int(x, y);
            this.elevation = elevation;
            this.terrain = terrain;
            walkable = terrain != TerrainType.Water && terrain != TerrainType.Lava;
            moveCost = terrain == TerrainType.Forest ? 2 : 1;
            cover = 0;
            hazard = terrain == TerrainType.Lava ? HazardType.Fire : HazardType.None;
        }
    }
}
