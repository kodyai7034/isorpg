using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IsoRPG.Core;
using IsoRPG.Map;

namespace IsoRPG.Units
{
    /// <summary>
    /// MonoBehaviour that visually represents a <see cref="UnitInstance"/> on the battle grid.
    /// Subscribes to unit events and updates sprite position, facing, and sorting order.
    ///
    /// Contains NO game logic — purely visual presentation.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class UnitView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private UnitInstance _unit;
        private BattleMapData _map;

        /// <summary>The data model this view is bound to.</summary>
        public UnitInstance Unit => _unit;

        /// <summary>
        /// Bind this view to a unit instance. Subscribes to unit events.
        /// Must be called once after instantiation.
        /// </summary>
        /// <param name="unit">The unit data to bind to.</param>
        /// <param name="map">Map data for elevation lookups during position updates.</param>
        public void Initialize(UnitInstance unit, BattleMapData map)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _unit = unit;
            _map = map;

            // Set initial position
            UpdateWorldPosition(unit.GridPosition);

            // Color by team
            _renderer.color = unit.Team switch
            {
                0 => new Color(0.5f, 0.7f, 1f),   // blue tint for player
                1 => new Color(1f, 0.5f, 0.5f),    // red tint for enemy
                _ => Color.white
            };

            // Subscribe to events
            unit.OnPositionChanged += OnPositionChanged;
            unit.OnDied += OnUnitDied;

            gameObject.name = $"Unit_{unit.Name}_{unit.Id}";
        }

        private void OnDestroy()
        {
            if (_unit == null) return;
            _unit.OnPositionChanged -= OnPositionChanged;
            _unit.OnDied -= OnUnitDied;
        }

        private void OnPositionChanged(Vector2Int from, Vector2Int to)
        {
            UpdateWorldPosition(to);
        }

        private void OnUnitDied()
        {
            // Placeholder: hide sprite. Full death animation in System 6.
            if (_renderer != null)
                _renderer.enabled = false;
        }

        private void UpdateWorldPosition(Vector2Int gridPos)
        {
            int elevation = _map != null ? _map.GetElevation(gridPos) : 0;
            var worldPos = IsoMath.GridToWorld(gridPos, elevation);

            // Offset Y slightly so unit renders above the tile
            worldPos.y += IsoMath.TileHeightHalf * 0.5f;

            transform.position = worldPos;

            // Sorting: units sort above tiles at same depth
            _renderer.sortingOrder = IsoMath.CalculateSortingOrder(
                gridPos.x, gridPos.y, elevation) + 1;
        }

        /// <summary>
        /// Animate movement along a path of grid positions.
        /// </summary>
        /// <param name="path">Ordered list of grid positions (excluding start).</param>
        /// <param name="map">Map data for elevation lookups.</param>
        /// <param name="speed">Movement speed in tiles per second.</param>
        /// <returns>Coroutine handle.</returns>
        public Coroutine AnimateMovement(List<Vector2Int> path, BattleMapData map, float speed = 4f)
        {
            return StartCoroutine(AnimateMovementCoroutine(path, map, speed));
        }

        private IEnumerator AnimateMovementCoroutine(List<Vector2Int> path, BattleMapData map, float speed)
        {
            foreach (var pos in path)
            {
                int elevation = map.GetElevation(pos);
                var target = IsoMath.GridToWorld(pos, elevation);
                target.y += IsoMath.TileHeightHalf * 0.5f;

                while (Vector3.Distance(transform.position, target) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position, target, speed * IsoMath.TileWidth * Time.deltaTime);

                    // Update sorting during movement
                    _renderer.sortingOrder = IsoMath.CalculateSortingOrder(
                        pos.x, pos.y, elevation) + 1;

                    yield return null;
                }

                transform.position = target;
            }
        }
    }
}
