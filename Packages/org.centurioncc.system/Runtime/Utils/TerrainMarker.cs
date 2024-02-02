using UdonSharp;
using UnityEngine;
using VRC.Udon.Serialization.OdinSerializer;

namespace CenturionCC.System.Utils
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [RequireComponent(typeof(Terrain))]
    public class TerrainMarker : ObjectMarkerBase
    {
        [SerializeField]
        public ObjectType[] correspondingObjectType;
        [SerializeField]
        public float[] correspondingSpeedMultiplier;
        [SerializeField]
        public Texture2D[] correspondingTextureHint;
        /// <summary>
        /// 3-dimensional array preprocessed into 1 dimension
        /// Most significant layer index at position is stored
        /// To calculate proper index:
        /// <code>y * width + x</code>
        /// </summary>
        [SerializeField] [HideInInspector]
        public int[] terrainDataSignificantLayer;
        [SerializeField]
        public Vector3 terrainDataSize;
        [SerializeField]
        public int terrainDataHeight;
        [SerializeField]
        public int terrainDataWidth;
        [SerializeField]
        public int terrainTextureCount;
        private int _lastIndex;

        private ObjectType _lastQueriedObjectType;
        private string[] _lastQueriedTags;
        private float _lastQueriedWalkingSpeedMultiplier;
        [OdinSerialize]
        public string[][] correspondingTags;

        public override ObjectType ObjectType => _lastQueriedObjectType;
        public override float ObjectWeight => 1;
        public override float WalkingSpeedMultiplier => _lastQueriedWalkingSpeedMultiplier;
        public override string[] Tags => _lastQueriedTags;

        public string[] PreviousTags { get; private set; }

        private void Start()
        {
            UpdateObjectMarkerInfo(0);
        }

        /// <summary>
        /// Returns most significant alphamap index at given world position
        /// </summary>
        /// <param name="worldPos">Point to check most significant ObjectType</param>
        /// <returns>-1 if failed to retrieve due to world pos being outside of Terrain. otherwise most significant alphamap index at given world position.</returns>
        public int GetIndexAtPosition(Vector3 worldPos)
        {
            var relPos = worldPos - transform.position /*- Vector3.one * 2F */;
            var alphamapCoordinate = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt((relPos.x / terrainDataSize.x) * terrainDataWidth), 0,
                    terrainDataWidth - 1),
                Mathf.Clamp(Mathf.RoundToInt((relPos.z / terrainDataSize.z) * terrainDataHeight), 0,
                    terrainDataHeight - 1)
            );

            if (alphamapCoordinate.x >= terrainDataWidth || alphamapCoordinate.y >= terrainDataHeight)
            {
                Debug.LogError(
                    $"[TerrainMarker-({name})] Accessed terrain alphamap coordinate was out of bounds: {alphamapCoordinate.ToString()}, {terrainDataWidth}:{terrainDataHeight}");
                return -1;
            }

            var coordinateIndex = (alphamapCoordinate.y * terrainDataWidth) + (alphamapCoordinate.x);
            if (terrainDataSignificantLayer.Length < coordinateIndex)
            {
                Debug.LogError(
                    $"[TerrainMarker-{name}] Accessing terrain alphamap index is out of bounds: {alphamapCoordinate.ToString()}, {coordinateIndex}, {terrainDataSignificantLayer.Length}");
                return -1;
            }

            var mostSignificantLayerIndex = terrainDataSignificantLayer[coordinateIndex];

            // Somehow corresponding type array was smaller than significant index
            if (correspondingObjectType.Length < mostSignificantLayerIndex)
            {
                Debug.LogError(
                    $"[TerrainMarker-{name}] Corresponding data is out of bounds: {mostSignificantLayerIndex}");
                return -1;
            }

#if CENTURIONSYSTEM_TERRAINMARKER_LOGGING || CENTURIONSYSTEM_VERBOSE_LOGGING
            Debug.Log(
                $"[TerrainMarker-{name}] Coordinate: {alphamapCoordinate.ToString()}: {coordinateIndex}. {mostSignificantLayerIndex}");
#endif

            return mostSignificantLayerIndex;
        }

        /// <summary>
        /// Updates object marker info by index
        /// </summary>
        /// <param name="index">Index of alphamap</param>
        /// <returns>true if index was changed from last assigned, false if unchanged.</returns>
        public bool UpdateObjectMarkerInfo(int index)
        {
            if (index <= -1 || index >= terrainTextureCount)
            {
                Debug.LogError($"[TerrainMarker-({name})] Index out of bounds: {index}. max: {terrainTextureCount}");
                return false;
            }

            var lastIndex = _lastIndex;
            PreviousTags = _lastQueriedTags;
            _lastIndex = index;
            _lastQueriedObjectType = correspondingObjectType[index];
            _lastQueriedWalkingSpeedMultiplier = correspondingSpeedMultiplier[index];
            _lastQueriedTags = correspondingTags[index];
            return lastIndex != _lastIndex;
        }

        public bool UpdateObjectMarkerInfo(Vector3 worldPos)
        {
            return UpdateObjectMarkerInfo(GetIndexAtPosition(worldPos));
        }
    }
}