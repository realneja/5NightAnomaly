using UnityEngine;
using UnityEngine.Tilemaps;

namespace FiveNightAnomaly.World
{
    /// <summary>
    /// The lane tiles that characters may stand on, painted on this Tilemap with the MarkerWalkable tile. Paint or erase
    /// tiles (Tile Palette "Lane") to shape where the Pengembara can walk left, right, up and down. The green tiles only
    /// show in the Editor: the renderer is switched off in Play mode, so the ground art underneath stays as it is.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public sealed class WalkableArea : MonoBehaviour
    {
        private Tilemap tilemap;

        private void Awake()
        {
            tilemap = GetComponent<Tilemap>();
            if (TryGetComponent(out TilemapRenderer tilemapRenderer))
                tilemapRenderer.enabled = false;
        }

        /// <summary>True when all four corners of the footprint (centre plus or minus halfSize) lie on painted tiles.</summary>
        public bool Contains(Vector2 centre, Vector2 halfSize)
        {
            return Has(centre + new Vector2(-halfSize.x, -halfSize.y)) && Has(centre + new Vector2(halfSize.x, -halfSize.y))
                && Has(centre + new Vector2(-halfSize.x, halfSize.y)) && Has(centre + new Vector2(halfSize.x, halfSize.y));
        }

        private bool Has(Vector2 point) => tilemap.HasTile(tilemap.WorldToCell(point));
    }
}
