using UnityEngine;
using IsoRPG.Core;

namespace IsoRPG.Map
{
    public class IsometricGrid : MonoBehaviour
    {
        [SerializeField] private BattleMapData mapData;
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 0.5f);

        private GameObject[,] _tileObjects;
        private Vector2Int _hoveredTile = new Vector2Int(-1, -1);
        private SpriteRenderer _hoveredRenderer;

        public BattleMapData MapData => mapData;

        public void LoadMap(BattleMapData data)
        {
            mapData = data;
            ClearMap();
            BuildMap();
        }

        private void Start()
        {
            if (mapData != null)
                BuildMap();
        }

        private void Update()
        {
            HandleMouseHover();
        }

        private void BuildMap()
        {
            _tileObjects = new GameObject[mapData.width, mapData.height];

            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    var tile = mapData.GetTile(x, y);
                    var worldPos = IsoMath.GridToWorld(x, y, tile.elevation);

                    var tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                    tileObj.name = $"Tile_{x}_{y}";

                    var sr = tileObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = IsoMath.CalculateSortingOrder(x, y, tile.elevation);
                        sr.color = GetTerrainColor(tile.terrain);
                    }

                    _tileObjects[x, y] = tileObj;
                }
            }
        }

        private void ClearMap()
        {
            if (_tileObjects == null) return;
            foreach (var obj in _tileObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _tileObjects = null;
        }

        private void HandleMouseHover()
        {
            if (mapData == null || _tileObjects == null) return;

            var mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var gridPos = IsoMath.WorldToGrid(mouseWorld);

            if (gridPos != _hoveredTile)
            {
                // Unhighlight previous
                if (_hoveredRenderer != null)
                {
                    var prevTile = mapData.GetTile(_hoveredTile);
                    _hoveredRenderer.color = GetTerrainColor(prevTile.terrain);
                }

                _hoveredTile = gridPos;

                // Highlight new
                if (mapData.InBounds(gridPos))
                {
                    var obj = _tileObjects[gridPos.x, gridPos.y];
                    _hoveredRenderer = obj.GetComponent<SpriteRenderer>();
                    if (_hoveredRenderer != null)
                        _hoveredRenderer.color = highlightColor;
                }
                else
                {
                    _hoveredRenderer = null;
                }
            }
        }

        private Color GetTerrainColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Grass => new Color(0.4f, 0.7f, 0.3f),
                TerrainType.Stone => new Color(0.6f, 0.6f, 0.6f),
                TerrainType.Water => new Color(0.3f, 0.5f, 0.8f),
                TerrainType.Sand => new Color(0.9f, 0.85f, 0.6f),
                TerrainType.Lava => new Color(0.9f, 0.3f, 0.1f),
                TerrainType.Forest => new Color(0.2f, 0.5f, 0.2f),
                _ => Color.white,
            };
        }

        public Vector2Int GetHoveredTile() => _hoveredTile;
    }
}
