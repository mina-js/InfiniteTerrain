using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    const float viewerThresholdForUpdate = 25f;
    const float squareViewerThresholdForUpdate = viewerThresholdForUpdate * viewerThresholdForUpdate;


    public static float maxViewDst;
    public Transform viewer;

    public static Vector2 viewerPosition;
    Vector2 oldViewerPosition;
    int chunkSize;
    int visibleChunksInView;

    public Material mapMaterial;

    public LODInfo[] detailLevels;

    Dictionary<Vector2, TerrainChunk> chunkMap = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    static MapGenerator mapGenerator;

    // Start is called before the first frame update
    void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDistanceThresh;
        visibleChunksInView = Mathf.RoundToInt(maxViewDst / chunkSize);
        mapGenerator = FindObjectOfType<MapGenerator>();
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        if((viewerPosition - oldViewerPosition).sqrMagnitude > squareViewerThresholdForUpdate)
        {
            UpdateVisibleChunks();
            oldViewerPosition = viewerPosition;
        }
    }

    // Update is called once per frame
    void UpdateVisibleChunks()
    {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisibile(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);


        for(int yOffset = -visibleChunksInView; yOffset <= visibleChunksInView; yOffset++)
        {
            for (int xOffset = -visibleChunksInView; xOffset <= visibleChunksInView; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (chunkMap.ContainsKey(viewedChunkCoord))
                {
                    chunkMap[viewedChunkCoord].Update(); //not Unity's update
                    if (chunkMap[viewedChunkCoord].isVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(chunkMap[viewedChunkCoord]);
                    }
                } else
                {
                    chunkMap.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial, detailLevels));
                }
            }
        }
    }

    public class TerrainChunk
    {
        Vector2 position;
        GameObject meshObject;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        int prevLODIndex = -1;

        MapData mapData;
        bool mapDataReceived;

        public TerrainChunk(Vector2 coord, int size, Transform parent, Material material, LODInfo[] detailLevels)
        {

            this.detailLevels = detailLevels;

            position = coord * size;

            bounds = new Bounds(position, Vector2.one * size);

            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            meshObject = new GameObject("Terrain Stuff");//GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshObject.transform.position = positionV3;
            //meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            SetVisibile(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, Update);
            }

            mapGenerator.RequestMapData(onMapDataReceived);

        }

        void onMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            Update();
            //mapGenerator.RequestMeshData(mapData, onMeshDataReceived);
        }

        void onMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void Update()
        {

            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDistanceThresh)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != prevLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            prevLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }
                SetVisibile(visible);
            }
            
        }

        public void SetVisibile(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool isVisible()
        {
            return meshObject.activeSelf;
        }
    }

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void onMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, onMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThresh;

    }
}
