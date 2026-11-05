using System.Linq;
using NUnit.Framework;
using PocketBorough;
using UnityEngine;

public class BoroughTests
{
    [Test] public void FootprintMustFitEntireOpening()
    {Assert.IsFalse(BoroughRules.Fits(1,1));Assert.IsTrue(BoroughRules.Fits(1.1f,1));Assert.IsFalse(BoroughRules.Fits(.88f,3));}
    [Test] public void GrowthIsMonotonicBoundedAndStartsSmall()
    {Assert.AreEqual(BoroughRules.InitialRadius,BoroughRules.Radius(0),.001f);float previous=0;for(int i=0;i<10000;i+=11){float r=BoroughRules.Radius(i);Assert.GreaterOrEqual(r,previous);Assert.LessOrEqual(r,11.5f);previous=r;}Assert.AreEqual(BoroughRules.Radius(0),BoroughRules.Radius(-10));}
    [Test] public void EqualAndSlightlyLargerRivalsCannotEatEachOther()
    {Assert.IsFalse(BoroughRules.CanEatHole(1,1));Assert.IsFalse(BoroughRules.CanEatHole(1.3f,1));Assert.IsTrue(BoroughRules.CanEatHole(1.4f,1));}
    [Test] public void AllFortyFiveOriginalMeshesHaveFiniteGeometryAndNormals()
    {
        for(int i=0;i<45;i++)
        {var mesh=i<15?MeshWorkshop.Building(i):i<25?MeshWorkshop.Vehicle(i-15):MeshWorkshop.Prop(i-25);Assert.Greater(mesh.triangles.Length,120,mesh.name);Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);Assert.IsTrue(mesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)));Assert.Greater(mesh.bounds.size.x,.1f);Assert.Greater(mesh.bounds.size.y,.1f);Assert.Greater(mesh.bounds.size.z,.05f);Assert.GreaterOrEqual(mesh.bounds.min.y,-.001f);Object.DestroyImmediate(mesh);}
    }
    [Test] public void CatalogContainsModelsMaterialsAudioAndFont()
    {var c=Resources.Load<BoroughCatalog>("BoroughCatalog");Assert.IsNotNull(c);Assert.AreEqual(45,c.models.Length);foreach(var d in c.models){Assert.IsNotNull(d.prefab,d.title);Assert.Greater(d.footprint,0);Assert.Greater(d.mass,0);Assert.Greater(d.points,0);Assert.IsNotNull(d.prefab.GetComponent<BoxCollider>());}Assert.IsNotNull(c.font);Assert.IsNotNull(c.music);Assert.AreEqual(9,c.sounds.Length);Assert.IsTrue(c.sounds.All(a=>a));}
    [Test] public void PoolResetRestoresObjectsAndPreventsDuplicateRewards()
    {
        var c=Resources.Load<BoroughCatalog>("BoroughCatalog");var root=new GameObject("Test world");
        try
        {
            var world=new ArenaWorld(root.transform,c);var hole=new HoleAgent("Test",Color.red,true,root.transform,c);hole.Reset(new(-48,0,-54));
            var small=world.objects.First(o=>BoroughRules.Fits(hole.Radius,o.model.footprint));hole.Position=small.home;
            world.TryConsume(hole);int claimed=world.falling.Count;Assert.Greater(claimed,0);world.TryConsume(hole);Assert.AreEqual(claimed,world.falling.Count);
            int rewards=0;world.Animate(1,(h,o)=>rewards++);Assert.AreEqual(claimed,rewards);world.Animate(1,(h,o)=>rewards++);Assert.AreEqual(claimed,rewards);
            int restored=world.Restock(new System.Collections.Generic.List<HoleAgent>());Assert.AreEqual(claimed,restored);Assert.AreEqual(world.objects.Count,world.Remaining);Assert.AreEqual(0,world.Restock(new System.Collections.Generic.List<HoleAgent>()));
            world.Reset();Assert.AreEqual(world.objects.Count,world.Remaining);Assert.AreEqual(0,world.falling.Count);Assert.IsTrue(world.objects.All(o=>o.available&&!o.falling&&o.transform.gameObject.activeSelf&&o.transform.position==o.home));
        }
        finally{Object.DestroyImmediate(root);}
    }
}
