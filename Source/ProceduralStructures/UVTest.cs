using FlaxEngine;

namespace Game.ProceduralStructures;

public class UVTest : Script
{
    public Material Material;
    public float Width = 400;
    public float Height = 300;
    public float Length = 300f;
    public float UScale = 0.01f;
    public float VScale = 1;
    public float UOffset = 0;
    public bool Displace = false;
    public Vector3 Displacement = new(5, 5, 5);
    private Model _tempModel;
    private MeshCollider _collider;
    
    /// <inheritdoc/>
    public override void OnEnable()
    {
        CreateShape();
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        Destroy(ref _tempModel);
    }

    public override void OnUpdate()
    {
        if (Input.GetMouseButtonUp(MouseButton.Left))
        {
            var pos = Input.MousePosition;
            var ray = Camera.MainCamera.ConvertMouseToRay(pos);
            if (Physics.RayCast(ray.Position, ray.Direction, out var hitInfo, 10000, layerMask: ~(1U << 1)))
            {
                if (hitInfo.Collider.Equals(_collider))
                {
                    Debug.Log("Triangle #" + hitInfo.FaceIndex + " hit, normal = " + hitInfo.Normal);
                    CreateShape();
                }
            }
        }
    }

    // create a simple arc for debugging cylinder texture projection
    private void CreateShape()
    {
        var ps = new ProceduralStructure();
        
        // borrow initial shape from the cave builder
        var vertices = ps.ConstructTunnelShape(Width, Height);
        var centerFront = Builder.FindCentroid(vertices);
        var mo = new MeshObject
        {
            Material = Material
        };
        var front = mo.AddRange(vertices);
        var back = mo.AddRange(Builder.MoveVertices(vertices, Vector3.Backward * Length));
        mo.BridgeEdgeLoops(front, back);
        if (Displace)
            mo.RandomizeVertices(Displacement);
        mo.FlipNormals();
        mo.SetUVTunnelProjection(centerFront, Vector3.Backward, UOffset, UScale, VScale);

        Model model;
        if (_tempModel != null)
        {
            model = _tempModel;
        }
        else
        {
            model = Content.CreateVirtualAsset<Model>();
            model.SetupLODs(new[] { 1 });
            _tempModel = model;
            model.MaterialSlots[0].Material = mo.Material;
        }
        
        mo.BuildMesh(model.LODs[0].Meshes[0]);
        var modelActor = Actor.GetOrAddChild<StaticModel>();
        modelActor.Name = "Generated Mesh";
        modelActor.Model = model;

        _collider = Actor.GetOrAddChild<MeshCollider>();
        var collisionData = _collider.CollisionData;
        if (collisionData == null)
        {
            collisionData = Content.CreateVirtualAsset<CollisionData>();
            _collider.CollisionData = collisionData;
        }
        mo.UpdateCollisionData(collisionData);
        _collider.CollisionData = collisionData;
    }
}
